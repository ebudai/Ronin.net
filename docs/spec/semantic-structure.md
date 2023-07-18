# 4 Semantic Structure

## 4.1 Datum

- *identifier*
- *mutability* (must have one and only one)
- *modifier* (any combination)
- *datatype*
- *initializer* is a ***reference***

#### 4.1.1 Mutability

- constant
- variable
- reactive

#### 4.1.2 Modifier

- compiled
- shared
- persistent

## 4.2 Datatype

- possibly optional
- *identifier*
- *algebra*
- *context*
- *initializer* is a ***function***

#### 4.2.1 Algebra

- *bases* is a list of ***datatype***
- *unions* is a list of ***datatype***

## 4.3 Function

- *identifier*
- *resolves to* is a ***datatype***
- *instructions* is a list of ***instruction***
- *context*

## 4.4 Instruction

- *target* is a ***function*** or ***datum***
- *inputs*

#### 4.4.1 Assignment
A type of ***instruction***

- *destination* is a ***datum***

## 4.5 Identifier

- list of ***words***, ***value***, or ***parameters***

## 4.6 Context

- *functions* is a list of ***function***
- *data* is a list of ***datum***
- *datatypes* is a list of ***datatype***
- *parent* is a ***context***
- *children* is a list of ***context***

## 4.7 Module

- *name* is an ***identifier***
- *initializer* is a list of ***instruction***
- *context*

## 4.8 Error
Represents a *compile-time* error resulting from an invalid combination of syntax.