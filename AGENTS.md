# Working in FocusRGB-Plugin-SignalRGB

Follow the repository workflow, attribution rules, conventions, and slice documentation from
the peer `FocusRGB/FocusRGB` repository. Those instructions take precedence over assistant
skills when they conflict.

The code graph is workspace-wide at the workspace folder's `build/graphify-out/` (the parent of
this clone; see the workspace `AGENTS.md`). Query from the workspace folder, or from the repository
root pass `--graph ../build/graphify-out/graph.json`. Confirm graph findings in source because the
graph can lag. Never run `graphify update` inside a clone; run it only from the workspace folder.
Never run `graphify hook install` in this repo. Never run `graphify extract` or semantic/deep modes
unless the owner asks because they call an LLM. Never commit graph output.
