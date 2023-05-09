# LogicScript

A simple DSL for programming logic components.

If you are using Visual Studio Code, you can download [an extension](https://marketplace.visualstudio.com/items?itemName=pipe01.logicscript-lang) to make editing LogicScript files much easier.

* [Sample code](#sample-code)
* [Syntax reference](#syntax-reference)

# Sample code

Here are a few snippets of LogicScript code to give you a feel for the language:

```
// This script doesn't do anything useful

input a
input b
input'3 data

output z
output'2 out

const myconst = 123

reg'3 mem

assign out = 3
assign z = (myconst)'1

startup
    @print "Hello world"
end

when *
    if 1 == 2
        @print "Not equal"
    else
        @print "Equal"
    end

    local $test = 1010b
    @print "Test: $test hex: $test:x binary: $test:b"

    $test '= $test + 1
    @print $test

    for $i to 5
        $test = $test - $i
    end

    local $mul = $test * 2

    out = ($mul)'2
end
```

Simple counter:
```
input add1
output'4 result

reg'4 val

when add1
    val '= val + 1
    result = val
end
```

Multiplexer:
```
input sel
input'2 data
output out

assign out = sel ? data[0] : data[1]
```

# Syntax reference

A script is executed sequentially from top to bottom. The script is composed of port declarations, const declarations and code blocks.

## Concepts

### Values

All values are unsigned numbers with a specified bit length, maximum 64 bits.

They can be defined in one of three formats:

* Decimal format, which is just their decimal representation
    * `0`, `23`, `100`
* Binary format, which is one or more zeroes or ones followed by a `b`
    * `0b`, `1101b`
* Hexadecimal format, which is a `0x` followed by one or more hexadecimal characters
    * `0x0`, `0xF`, `0xDeAdBeEf`

### Ports

The machine can interact with its surroundings through the use of inputs and outputs, and it can additionally store an arbitrary amount of data using registers.

Inputs can only be read from, and outputs can only be written to. They have a size of 1 bit, however they can be grouped together and be represented as a single number. Registers can be written to and read from, they can have a size from 1 to 64 bits and they persist their value.

## Top-level declarations
### Ports

A LogicScript script must define the inputs, outputs and registers it wants to use, along with the bit size of each of them.

They are declared simply by stating their kind, size and name, e.g.:

```
input'3 data
out'2 z
reg'10 mem
```

The size specification (`'n`) can be skipped, in which case a size of `1` will be assumed for inputs and outputs, and a size of `64` will be assumed for registers.

### Constants

A constant value can be declared at the top level, and it can contain a number of any size. Its value must be constant, thus it can only refer to other constants in its declaration.

```
const myConst = 123
```

### Comments

There are two types of comments: line comments that start with a `//` and span to the end of the line, and block comments that start with `/*` and span until a `*/` is found.

### Blocks
#### `when`

```logicscript
when a == 3
    // Runs if a equals 3
end

when *
    // Always gets executed
end
```

The `when` block will run its body if its condition value is [truthy](#truthy-values). Alternatively, if its condition is a single asterisk (`*`), it will always be run.

#### `startup`

```logicscript
startup
    // Runs only once at startup
end
```

The `startup` block will only run its body the first time the machine is updated, up to implementation.

#### `assign`

```logicscript
assign z = a | b
```

The `assign` block is a shortcut for a `when *` block, its body must be an assignment and it will always be run.

## Code blocks

The body of the blocks mentioned above consists of multiple statements, one per line (except the `assign` block, which only accepts a single statement). These statements can optionally end with a semicolon (`;`), which allows for multiple statements in a single line.

### Expressions

#### Port references

To read readable (input and register) ports' value, you can use the same name that was specified in the declaration.

#### Operators

There are three kinds of operators: unary and binary operators that have one or two operands respectively, and a ternary operator.

##### Binary operators

Binary operators have one operand on each side. These are the operators that are currently implemented, in order of precedence:

* `|`, `&`: performs a bitwise OR or AND operation of both operands respectively.
* `^`: performs a bitwise XOR operation of both operands.
* `**`: raises the left number to the power of the right number.
* `+`, `-`: performs an addition or subtraction of both operands respectively.
* `*`, `/`: performs a multiplication or division of both operands respectively.
+ `%`: returns the remainder of dividing both operands.
* `<<`, `>>`: shifts the right operand N positions to the left or right respectively, where N is the value of the second operand.
* `==`, `>`, `<`: compares both operands and returns a single bit.

#### Unary operators

* `!x` or `~x`: returns a bitwise negation of all the bits of `x`.
* `len(x)`: returns the bit length of `x`.
* `allOnes(x)`: returns `1` if all the bits of `x` are set to ones, and `0` otherwise.
* ~`rise(x)`: not implemented~
* ~`fall(x)`: not implemented~
* ~`change(x)`: not implemented~

#### Ternary operator

The ternary operator has a condition operand that determines which of the other two operands will be returned. If it is [truthy](#truthy-values), the first operand will be returned, otherwise the second will be returned.

```lua
x ? a : b
```

#### Slicing

The slice expression can be used to get a number consisting of a subset of another number's bits, starting from the left or the right. If the length is unspecified then 1 will be assumed.

In order to specify the starting reference, a `<` or `>` character can be inserted after the opening bracket to indicate "left" or "right" respectively; if neither is used, the latter will be assumed. The syntax is as follows:

```js
[0]     // Get 1 bit starting from the 1st position from the right (the offset is inclusive)
[0,3]   // Get 3 bits starting from the 1st position from the right
[>4,3]  // Get 3 bits starting from the 5th position from the right
[<2,2]  // Get 2 bits starting from the 3rd position from the left
```

#### Truncation

In order to fit a number into a slot of a smaller length, it can be truncated to a shorter bit length. If the new bit size is greater than the operand's current bit size, it will be padded with zeroes.

```
(123)'4    // Truncates the number into a 4 bit number
(3)'10     // Fits the number into a 10 bit number, padding with zeroes
```

There's also a shortcut truncation assignment, which will truncate the right side to fit into the left side.

```
local $var'4

$var '= 100 // Will truncate to 4 bits
```

### Statements

#### Locals declaration

Code blocks can include local variable declarations of the form `local $name` or `local $name = initializer`. Additionally, their bit size can be specified using a bit size declaration after its name: `'n` where `n` is the number of bits. If the initializer is not present, the local will be set to `0` and its bit size must be explicitly declared.

#### Assignment

Assignments set a writable (output or register) port or local to a given value. The value must be small enough to fit into the port or local, otherwise a parse-time error will be raised.

```lua
port = /*value*/
$var = /*value*/
```

#### If statement

Conditionally runs a list of statements if the condition expression evaluates to a [truthy](#truthy-values) value. It can optionally be followed by one or more `else if` statements, and finally an `else` statement.

```lua
if /*condition*/
    // ...
else if /*condition*/
    // ...
else
    // ...
end
```

#### For statement

Runs a list of statements a number of times, as defined by the "from" and "to" values. It assigns an increasing value to a local, which will be declared if it isn't already. The "from" value is inclusive, while the "to" value isn't. If the former is not specified, `0` will be assumed.

```lua
for $i from 0 to 5
    // ...
end
```

#### While statement

Runs a list of statements until the condition expression is no longer [truthy](#truthy-values).

```lua
while 1
    // ...
end
```

#### Break statement

When used inside a `for` or `while` loop, it exits that loop. When used inside a `when` or `startup` block, it exits that block.

#### Tasks

Task statements are used to perform actions on the machine.

##### Print task

The print task allows you to print a single value, or a string that can interpolate other values.

When passing a string it must be wrapped in double quotes (`"`), and it can contain interpolated locals in the `$name` form. Interpolated locals can be suffixed by `:b` or `:x`, which will format their value in binary or hexadecimal respectively.

```lua
@print $myvar
@print "The value of myvar is: $myvar, or $myvar:b in binary"
```

## Miscellaneous concepts

### Truthy values

A value is considered truthy if it is not equal to zero.
