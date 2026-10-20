# Working in FocusRGB-Plugin-SignalRGB

Follow the repository workflow, attribution rules, conventions, and slice documentation from
the peer `FocusRGB/FocusRGB` repository. Those instructions take precedence over assistant
skills when they conflict.

The code graph is at `graphify-out/` and is ignored; local git hooks refresh it automatically.
Before broad searches, orient with `graphify query "<question>"`, `graphify explain "<symbol>"`,
or `graphify path "A" "B"`, keeping `--budget` small on each command. Confirm findings in the
actual source because the graph can lag. If it is stale or missing, run `graphify update .`
(no LLM). Never run `graphify extract` or semantic/deep modes unless the owner asks: they call
an LLM. Never commit `graphify-out/`.
