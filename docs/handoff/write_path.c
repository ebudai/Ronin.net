// The measurement the programmer correctly said was missing.
//
// My doc cited 3.6x from propagation.c. That benchmark compared a FIFO RING
// BUFFER against a double buffer. reactive_core.py implements NEITHER -- it
// uses a pending map keyed by var, holding only the latest value. That is
// latest-value semantics, same as the double buffer, so the 3.6x is not
// evidence for changing it. This measures the comparison that actually applies.
//
// Per propagation step, with W writes and R reads:
//   pending map   hash insert per write, iterate + copy at step, plain load read
//   double buffer array store per write, flip ONE index at step, indexed read
#define _GNU_SOURCE
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

static double now(void) {
    struct timespec ts; clock_gettime(CLOCK_MONOTONIC, &ts);
    return ts.tv_sec + ts.tv_nsec * 1e-9;
}

#define VARS 1024
#define STEPS 20000

// ------------------------------------------------------- pending map
// Open-addressed, the shape a Dictionary<Node,object> compiles to.
typedef struct { int key; double value; } entry_t;
static entry_t map[VARS * 2];
static int map_keys[VARS];
static int map_count;

static inline unsigned hash(int k) { return (unsigned)k * 2654435761u; }

static void map_put(int key, double v) {
    unsigned i = hash(key) & (VARS * 2 - 1);
    while (map[i].key != -1 && map[i].key != key) i = (i + 1) & (VARS * 2 - 1);
    if (map[i].key == -1) { map[i].key = key; map_keys[map_count++] = key; }
    map[i].value = v;
}
static double map_get(int key) {
    unsigned i = hash(key) & (VARS * 2 - 1);
    while (map[i].key != key) i = (i + 1) & (VARS * 2 - 1);
    return map[i].value;
}
static double pm_front[VARS];
static void map_step(void) {
    for (int i = 0; i < map_count; i++) {
        int k = map_keys[i];
        pm_front[k] = map_get(k);
    }
    for (int i = 0; i < map_count; i++) {
        unsigned j = hash(map_keys[i]) & (VARS * 2 - 1);
        while (map[j].key != map_keys[i]) j = (j + 1) & (VARS * 2 - 1);
        map[j].key = -1;
    }
    map_count = 0;
}

// ----------------------------------------------------- double buffer
static double db[VARS][2];
static int db_front;

static inline void db_put(int key, double v) { db[key][1 - db_front] = v; }
static inline void db_step(void) {
    // carry unwritten vars forward: the flip exposes the whole back plane
    for (int i = 0; i < VARS; i++) db[i][1 - db_front] = db[i][db_front];
    db_front = 1 - db_front;
}

volatile double sink;

static void bench(int writes, int reads) {
    double acc = 0, t;

    memset(map, -1, sizeof(map)); map_count = 0;
    t = now();
    for (int s = 0; s < STEPS; s++) {
        for (int w = 0; w < writes; w++) map_put(w, s + w);
        map_step();
        for (int r = 0; r < reads; r++) acc += pm_front[r & (VARS - 1)];
    }
    double t_map = (now() - t) / STEPS * 1e6;

    db_front = 0;
    t = now();
    for (int s = 0; s < STEPS; s++) {
        for (int w = 0; w < writes; w++) db_put(w, s + w);
        db_step();
        int f = db_front;
        for (int r = 0; r < reads; r++) acc += db[r & (VARS - 1)][f];
    }
    double t_db = (now() - t) / STEPS * 1e6;

    sink = acc;
    printf("  %6d %7d   %9.3f %9.3f   %7.2fx\n",
           writes, reads, t_map, t_db, t_map / t_db);
}

int main(void) {
    printf("  per propagation step, %d vars in the graph\n\n", VARS);
    printf("  %6s %7s   %9s %9s   %8s\n",
           "writes", "reads", "map (us)", "dbuf (us)", "map/dbuf");
    bench(1, 10);
    bench(1, 100);
    bench(4, 100);
    bench(16, 100);
    bench(64, 500);
    bench(256, 1000);
    printf("\n  A realistic frame writes a handful of sources and reads many\n");
    printf("  derived values -- the top rows. Note db_step must carry the\n");
    printf("  UNWRITTEN vars forward across the flip, which is O(vars), while\n");
    printf("  the map is O(writes). That is why the double buffer loses when\n");
    printf("  writes are few, which is the common case.\n");
    return 0;
}
