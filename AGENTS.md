## Code tools (CodeGraph + Memento)

This project's mod source (`modding/BepInExModsSource`) is indexed by CodeGraph (`.codegraph/` in that folder) and tracked by Memento for cross-session memory. Prefer these over graphify.

- For codebase questions inside `modding/BepInExModsSource`, start with the `codegraph_explore` MCP tool or `codegraph explore "<symbols or question>"` run from that folder. Both return the relevant symbols' verbatim source plus call paths and blast radius in one call — no grep/read loop. Use `codegraph status` for index health; the graph auto-syncs on file changes, so no manual update step is needed.
- Memento provides persistent memory across sessions. At the start of non-trivial work call `search_nodes` for project keywords; after decisions, fixes, or discoveries call `create_entities` / `add_observations` / `create_relations`. Database: `C:\Users\game\.local\share\memento\memory.db`. The first semantic `search_nodes` downloads the BGE-M3 model (~700 MB) once.
- When altering code, the supposed to be removed line/block of codes must be preserved by commenting it out. Provide context why it is obsolete and why we use the new code, also add a timestamp.
- Always use the karpathy-guidelines skill.

<!-- [2026-10-02 09:57] graphify is superseded by CodeGraph (code graph + queries) and Memento (persistent memory); the rules below are kept for reference only and should not drive new work. Re-enable only if the user explicitly asks for graphify.
## graphify

This project's mod source has a knowledge graph at `modding/BepInExModsSource/graphify-out/` with god nodes, community structure, and cross-file relationships. When working inside that folder, query paths resolve from there (`graphify query ...` run with `BepInExModsSource` as working directory). If you are elsewhere in the repo and the graph path is not found, run graphify from `modding/BepInExModsSource`.

When the user types `/graphify`, use the installed graphify skill or instructions before doing anything else.

Rules:
- For codebase questions, first run `graphify query "<question>"` (from `modding/BepInExModsSource`) when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- Dirty graphify-out/ files are expected after hooks or incremental updates; dirty graph files are not a reason to skip graphify. Only skip graphify if the task is about stale or incorrect graph output, or the user explicitly says not to use it.
- If `modding/BepInExModsSource/graphify-out/wiki/index.md` exists, use it for broad navigation instead of raw source browsing.
- Read `modding/BepInExModsSource/graphify-out/GRAPH_REPORT.md` only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code in `modding/BepInExModsSource`, run `graphify update .` there to keep the graph current (AST-only, no API cost).
- If able, use graphify before and after fixes.
-->

<!-- CODEGRAPH_START -->
## CodeGraph

In repositories indexed by CodeGraph (a `.codegraph/` directory exists at the repo root), reach for it BEFORE grep/find or reading files when you need to understand or locate code:

- **MCP tool** (when available): `codegraph_explore` answers most code questions in one call — the relevant symbols' verbatim source plus the call paths between them, including dynamic-dispatch hops grep can't follow. Name a file or symbol in the query to read its current line-numbered source. If it's listed but deferred, load it by name via tool search.
- **Shell** (always works): `codegraph explore "<symbol names or question>"` prints the same output.

If there is no `.codegraph/` directory, skip CodeGraph entirely — indexing is the user's decision.
<!-- CODEGRAPH_END -->
