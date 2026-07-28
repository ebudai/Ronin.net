// Three ways to get a written value from an owner to observing nodes.
//
// A reactive graph is read-heavy: ONE writer per shared var, MANY readers, and
// many reads per propagation step. So the read path is what matters.
#define _GNU_SOURCE
#include <stdio.h>
#include <stdlib.h>
#include <stdatomic.h>
#include <time.h>

static double now(void) {
    struct timespec ts; clock_gettime(CLOCK_MONOTONIC, &ts);
    return ts.tv_sec + ts.tv_nsec * 1e-9;
}

#define ITERS 10000000
#define QCAP  (1 << 14)

// ---------------------------------------------------------------- 1. queue
// Ring buffer, single producer, single consumer cursor, acquire/release only.
// A fair implementation, not a strawman.
typedef struct {
    _Alignas(64) atomic_ulong head;
    _Alignas(64) atomic_ulong tail;
    _Alignas(64) double slot[QCAP];
} queue_t;

static queue_t q;

static inline int q_push(double v) {
    unsigned long h = atomic_load_explicit(&q.head, memory_order_relaxed);
    unsigned long t = atomic_load_explicit(&q.tail, memory_order_acquire);
    if (h - t >= QCAP) return 0;                 // full: the reader fell behind
    q.slot[h & (QCAP - 1)] = v;
    atomic_store_explicit(&q.head, h + 1, memory_order_release);
    return 1;
}
static inline int q_pop(double *out) {
    unsigned long t = atomic_load_explicit(&q.tail, memory_order_relaxed);
    unsigned long h = atomic_load_explicit(&q.head, memory_order_acquire);
    if (t == h) return 0;
    *out = q.slot[t & (QCAP - 1)];
    atomic_store_explicit(&q.tail, t + 1, memory_order_release);
    return 1;
}

// ------------------------------------------------------------- 2. seqlock
typedef struct {
    _Alignas(64) atomic_uint version;
    double value;
} seqlock_t;

static seqlock_t sl;

static inline void sl_write(double v) {
    unsigned ver = atomic_load_explicit(&sl.version, memory_order_relaxed);
    atomic_store_explicit(&sl.version, ver + 1, memory_order_relaxed);
    atomic_thread_fence(memory_order_release);
    sl.value = v;
    atomic_thread_fence(memory_order_release);
    atomic_store_explicit(&sl.version, ver + 2, memory_order_relaxed);
}
static inline double sl_read(void) {
    unsigned a, b; double v;
    do {
        a = atomic_load_explicit(&sl.version, memory_order_acquire);
        v = sl.value;
        atomic_thread_fence(memory_order_acquire);
        b = atomic_load_explicit(&sl.version, memory_order_relaxed);
    } while ((a & 1u) || a != b);
    return v;
}

// ------------------------------------------------- 3. double buffer + flip
// Writer fills the back buffer. Propagation flips ONE index for the whole
// graph, so every reader sees a consistent generation and a read is a plain
// load with no atomic at all.
typedef struct { double buffer[2]; } cell_t;
static cell_t cells[8];
static _Alignas(64) atomic_int front;

static inline void db_write(int id, double v) {
    cells[id].buffer[1 - atomic_load_explicit(&front, memory_order_relaxed)] = v;
}
static inline double db_read(int id, int f) { return cells[id].buffer[f]; }

volatile double sink;

// Accumulate into a LOCAL, not into the volatile: a volatile store per
// iteration costs more than any of the read paths and would flatten the whole
// comparison. The volatile is written once at the end so nothing is dead.

int main(void) {
    double t, per, q_round, q_read, s_ns, d_ns;
    double out;

    double acc = 0;
    t = now();
    for (long i = 0; i < ITERS; i++) {
        if (q_push((double)i) == 0) { while (q_pop(&out)) { } }
        if (q_pop(&out)) acc += out;
    }
    q_round = (now() - t) / ITERS * 1e9;
    printf("  %-42s %6.2f ns/value\n", "queue: push + pop round trip", q_round);

    for (int k = 0; k < QCAP - 1; k++) q_push(1.0);
    t = now();
    for (long i = 0; i < ITERS; i++) {
        if (q_pop(&out)) acc += out;
        else for (int k = 0; k < QCAP - 1; k++) q_push(1.0);
    }
    q_read = (now() - t) / ITERS * 1e9;
    printf("  %-42s %6.2f ns/read\n", "queue: pop only (read side alone)", q_read);

    sl_write(3.5);
    t = now();
    for (long i = 0; i < ITERS; i++) acc += sl_read();
    s_ns = (now() - t) / ITERS * 1e9;
    printf("  %-42s %6.2f ns/read\n", "seqlock: read", s_ns);

    db_write(0, 7.5);
    atomic_store(&front, 1 - atomic_load(&front));
    int f = atomic_load_explicit(&front, memory_order_acquire);
    t = now();
    for (long i = 0; i < ITERS; i++) acc += db_read(0, f);
    d_ns = (now() - t) / ITERS * 1e9;
    printf("  %-42s %6.2f ns/read\n", "double buffer: read (plain load)", d_ns);

    sink = acc;
    printf("\n  read path, relative to a plain load:\n");
    printf("    double buffer   1.00x\n");
    printf("    seqlock        %5.2fx\n", s_ns / d_ns);
    printf("    queue (pop)    %5.2fx\n", q_read / d_ns);
    printf("\n  memory per shared var:\n");
    printf("    double buffer   %zu bytes, fixed\n", sizeof(cell_t));
    printf("    seqlock         %zu bytes, fixed\n", sizeof(seqlock_t));
    printf("    queue           %zu bytes, and it can still overflow\n", sizeof(queue_t));
    return 0;
}
