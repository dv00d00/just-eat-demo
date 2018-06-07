# Promised overview of actor model and kinesis

## What is actor / actor system

    pretty old concept form late 70s
    actors (objects) communicate with each other via messages
    actor processes one message at a time

## Existing .net libs

* **Orleans**
* Akka.net
* Proto actor
* Hopac
* F#'s mailbox handlers

## What it tries to solve

    stateless applications tend to outsource state management to database
    if database becomes bottleneck it needs to be scaled somehow (sharding, node size)
    it is easy, but costly, sometimes very costly

    managing state in concurrent environment is hard
        (locks, mutexes, etc)

    actor system exists to allow programmers to write stateful applications and not to worry about issues related to concurrent state management

    it is achieved via incoming requests queue
        out of which actor should received commands / queries

    classical implementation produces totally async code

        A -> B (send request)
        ...
        A <- B (send response)

    orleans provides a way to synchronize this communication (a bit of perf overhead, much more simpler to work with)
        and also means to implement backpressure 

    classical implementation also requires actor lifetime to be managed by programmer
        orleans manages actor lifetime

## Orleans terminology

    grain = C# class
    silo = machine
    cluster = bunch of machines talking to each other, balancing grains
    activation = instance of an grain (object)
    actor reference = address of an activation (referentially transparent) which can be used to talk to it

## Extra orleans features

    cloud agnostic
    managed runtime for actors
    streams
    persistent actor state (if you want)
    timers

## Demo orleans apps

## Kinesis

    append only message stream
    which can be red by multiple consumers
    each consumer can process stream at it own pace
    by batches of arbitrary size
    from any point of its lifetime

    using aws middleware gets same semantics as sqs / sns combo

    but also aws provided lambda subscription for kinesis events

    it means we are getting batching for free

    and in a case of our orderview materialization lambda
    we are getting real time message processing for cases when there is no load
    and a guaranty that we will not consume more than {batch size} messages at time under heavy load

## Why should we care

    we are using timer to trigger order view updates lambda
    it is setup to trigger once per minute
    from time to time batch processing takes more than a minute
    new timer tick triggers another batch processing
    which runs in parallel with previous one
    causing more pressure to elastic search
    it will take more time to process 2 batches in parallel because of load to es increased

## Kinesis streams also enables Complex Event Processing and SQL to query data streams

    So you will be able to write a query
    which will group order events by order id
    in a sliding window of {x} minutes
    and output one distinct id per minute

```SQL
SELECT STREAM ticker_symbol, COUNT(*) OVER TEN_SECOND_SLIDING_WINDOW AS ticker_symbol_count
FROM "SOURCE_SQL_STREAM_001"
WINDOW TEN_SECOND_SLIDING_WINDOW AS (
PARTITION BY ticker_symbol
RANGE INTERVAL '10' SECOND PRECEDING);
```

### And then it is possible to feed output of such sampler into an aws lambda
