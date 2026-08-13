# Reference

GENERATED from the language's own table — every entry below is the description
carried by the thing it describes. Editing this file does nothing: the next build
writes it again. Change the summary where the entry is declared.

The guide is the hand-written half, and answers «how do I do X». This answers
«what exactly is Y».

## error

The type of a failure, which every type admits and which admits nothing.

## false

Untruth.

See also: «true».

## for each «one word, or a bracketed name» in (_)

Runs its body once for each element, binding the element to a name.

    for each «one name» in (the list)

## list of (_)

A type whose values are lists of one element type.

    list of (a type)

## number

The type of numbers.

## old (_)

The value a reactive name held before this step.

    old (a reactive name)

Its argument must be a bare reactive name — «old (x + 1)» is not a previous value of anything.

## optional (_)

A type whose value may be absent.

    optional (a type)

## return

Ends the current body without an answer.

    return

An action, or a «when» body — where it ends the current firing and leaves the «when» in place. A body answers or it does not, and mixing the two forms is refused.

See also: «return (_)».

## return (_)

Ends the current body, carrying an answer out of it.

    return (the answer)

A function that answers. An action and a «when» body have nothing to answer, so they take the form with no argument.

See also: «return».

## stop

Removes this «when», so it does not fire again and stops costing anything.

    stop

Only inside a «when» body. To end the current firing and leave the «when» in place, write «return».

See also: «return».

## text

The type of text.

## true

Truth.

See also: «false».

## truth

The type of «true» and «false».

See also: «true», «false».
