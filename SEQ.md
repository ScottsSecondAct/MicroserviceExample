# Using Seq for Logs and Traces

Seq is a structured log and trace server that runs as part of the Docker stack. Every service in this system ships logs and OpenTelemetry traces to Seq automatically — no configuration needed beyond starting the stack.

**UI:** http://localhost:5341

---

## Getting started

1. Start the stack: `docker compose up --build -d`
2. Open http://localhost:5341 in your browser
3. You should immediately see log events streaming in from all services

If the Events tab is empty, check that all services are healthy:
```sh
docker compose ps
```

---

## The two main tabs

### Events (logs)

Every log line from every service appears here in real time. Each event is structured — fields like `Service`, `TraceId`, `StatusCode`, and `Elapsed` are first-class properties you can filter and sort on, not just text buried in a message string.

### Traces

Each HTTP request that enters the system creates a trace — a tree of spans showing exactly which services handled it and how long each step took. Use this tab to see cross-service waterfalls.

---

## Common things to look up

### See all errors right now

```
@Level = 'Error'
```

### Filter to one service

```
Service = 'AuthService'
```

Available values: `AuthService`, `UserManagementService`, `AccountService`, `ContactService`, `DealService`, `ActivityService`, `ReportingService`, `ApiGateway`

### Find a slow request

```
Elapsed > 500
```

`Elapsed` is in milliseconds and is added automatically by `UseSerilogRequestLogging()` on every HTTP request.

### See all logs for one request across every service

Every log event carries the OpenTelemetry `TraceId` of the request that produced it. To follow a single request across services:

1. Find any log line from that request
2. Click the `TraceId` value in the properties panel on the right
3. Seq filters to all events with that `TraceId` — across every service that handled it

Or type it directly:
```
TraceId = 'your-trace-id-here'
```

### See failed HTTP requests

```
StatusCode >= 400
```

### See everything that happened in the last 5 minutes

Use the time range picker in the top-right corner — set it to **Last 5 minutes** or type a relative range like `last 5 minutes` in the search bar.

---

## Following a request end-to-end

Here's an example using login, which crosses two services (ApiGateway → AuthService → UserManagementService):

1. Log in through the frontend
2. In Seq Events, filter: `Service = 'AuthService' and @Message like '%login%'`
3. Click the matching event to expand it
4. Copy the `TraceId` value
5. Clear the filter and search: `TraceId = '<paste>'`
6. You'll see the full chain: gateway request → auth login handler → role fetch call to UserManagementService → JWT issued

To see the same as a trace waterfall:

1. Click the **Traces** tab
2. Find the trace by time or paste the TraceId into the search
3. Click it — you'll see a timeline with each service's span, including the outbound HTTP call from AuthService to UserManagementService

---

## Trace vs log: when to use which

| Question | Use |
|---|---|
| What happened in what order? | Traces — the waterfall shows sequence and timing |
| What did the code say at a specific moment? | Events — log messages have context and detail |
| Why did this request fail? | Start in Traces to find where it broke, then switch to Events with that TraceId to read the error message |
| How long did each step take? | Traces — span durations are shown visually |
| What's erroring across the whole system? | Events — `@Level = 'Error'` gives a system-wide view |

---

## Signal vs noise

The services log at `Information` level by default, which includes every HTTP request. To focus on what matters:

**Hide health check noise:**
```
@Level = 'Error' or (Elapsed > 100 and RequestPath not like '%/health%')
```

**Only warnings and above:**
```
@Level in ['Warning', 'Error', 'Fatal']
```

**Save a filter** by clicking the bookmark icon next to the search bar — saved filters appear in the left sidebar for quick access.

---

## Seq data

Seq stores its data in the `seq-data` Docker volume. Logs persist across container restarts. To clear all Seq data:

```sh
docker compose down -v
docker compose up -d seq
```

Note: `-v` removes **all** volumes including databases — see README for details.
