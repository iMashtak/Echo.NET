﻿# Echo Framework

Simple event-based framework for developing high concurrent applications wth event-based domain model.

## Usage

### Events

There are two main concepts: `Event` and `Bus`. You can publish event on bus and then subscribe to the type of that event to somehow handle it. It is not required to handle every event on the bus.
