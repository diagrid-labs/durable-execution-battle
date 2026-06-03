---
layout: default
footer: |
  <span>Marc Duiker</span>
  <img src=".demo/assets/diagrid-logo.webp" alt="Diagrid" style="height: 20px; width: auto;">
---

# What is durable execution?

Code that survives **process crashes**, **host failures**, and **deploys** — without you writing the checkpointing or retry plumbing.

```python
async def process_order(order):
    await charge_payment(order)
    await reserve_inventory(order)
    await ship_package(order)
    await send_tracking_email(order)
```
