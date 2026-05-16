---
layout: default
---

# What is durable execution?

Code that survives **process crashes**, **host failures**, **deploys**, and **indefinite waits** — without you writing the checkpointing or retry plumbing.

```python
async def book_starship_tour(passenger):
    await charge_card(passenger)
    await sleep(30 * DAY)        # warp core breach? doesn't matter.
    await send_boarding_pass(passenger)
```

> *Imagine writing* `await sleep(30 days)` *and it just works — even if the warp core breaches mid-wait.*
