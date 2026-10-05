// graphify OpenCode V2 plugin
// Shows a knowledge-graph reminder before the first shell command of a session
// whenever a built graph exists, nudging agents toward graphify query/path/explain.
//
// IMPORTANT: keep the reminder string free of backticks and $(...) constructs;
// it is prepended to the user's command inside a double-quoted echo.
import { existsSync } from "fs";
import { join } from "path";

export default {
  id: "graphify",
  async setup(ctx) {
    const directory = ctx.location?.directory ?? process.cwd();
    let reminded = false;

    const candidates = [
      join(directory, "graphify-out", "graph.json"),
      join(directory, "modding", "BepInExModsSource", "graphify-out", "graph.json"),
    ];

    const registration = await ctx.shell.hook("create.before", (event) => {
      if (reminded) return;
      const graph = candidates.find((p) => existsSync(p));
      if (!graph) return;

      // Prepend a one-time nudge to the command (not '&&' so the reminder
      // never breaks the user's command on PowerShell 5.1).
      event.command =
        'echo "[graphify] knowledge graph at graphify-out/. For focused questions, run graphify query with your question (scoped subgraph, usually much smaller than GRAPH_REPORT.md) instead of grepping raw files. Read GRAPH_REPORT.md only for broad architecture context." ; ' +
        event.command;
      reminded = true;
    });

    return () => registration.dispose();
  },
};