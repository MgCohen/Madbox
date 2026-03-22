# Cursor CLI, ACP, and Cloud Agents API

Date: 2026-03-22  
Scope: Research notes for building custom agentic workflows with Cursor: interactive CLI, non-interactive mode, Agent Client Protocol (ACP), and the Cloud Agents HTTP API.

Sources (official):

- [Using Agent in CLI](https://cursor.com/docs/cli/using)
- [CLI parameters](https://cursor.com/docs/cli/reference/parameters)
- [ACP](https://cursor.com/docs/cli/acp)
- [Agent Client Protocol (spec)](https://agentclientprotocol.com/)
- [Cursor APIs overview](https://cursor.com/docs/api)
- [Cloud Agents API endpoints](https://cursor.com/docs/cloud-agent/api/endpoints)
- [Cloud Agents OpenAPI](https://cursor.com/docs-static/cloud-agents-openapi.yaml)

---

## 1. Cursor CLI (`agent`)

### Summary

Cursor ships a **CLI agent** (`agent`) so the same agent capabilities used in the editor can run in a terminal or be invoked from scripts. The CLI is documented as suitable for users of other editors as well as automation.

### Modes

Aligned with the editor:

- **Agent** (default): full tool access (files, search, terminal, web).
- **Plan**: planning; can ask clarifying questions (`--plan`, `--mode=plan`, or `/plan`).
- **Ask**: read-only exploration (`--mode=ask`, `/ask`).

### Non-interactive / scripting

For pipelines and CI-style invocation:

- Use **`-p` / `--print`** for non-interactive runs.
- Use **`--output-format`** for machine-friendly output (for example `text`, `json`, `stream-json`).

Relevant for tight loops: combine `-p` with a prompt so the process exits with a final response instead of an interactive REPL.

### Other notable behaviors (from docs)

- **MCP**: CLI respects project or user MCP configuration (for example `.cursor/mcp.json`).
- **Rules**: loads `.cursor/rules` and project-root `AGENTS.md` / `CLAUDE.md` like the editor.
- **Command approval**: interactive CLI may prompt before running terminal commands; non-interactive mode has full write access per docs.
- **Cloud handoff**: `-c` / `--cloud` or `&` prefix to send work to a cloud agent session.
- **Resume**: `--resume`, `agent resume`, `/resume`, or `agent ls` for conversation history.

### Example snippets

```bash
# Typical install (see Cursor install docs)
curl https://cursor.com/install -fsSL | bash
```

```bash
# Interactive agent chat
agent chat "find one bug and fix it"
```

```bash
# Non-interactive one-shot (scripting)
agent -p --output-format json "Summarize AGENTS.md in three bullets"
```

---

## 2. ACP (Agent Client Protocol)

### Is ACP “Cursor-native”?

**No as a standard, yes as an integration surface.** ACP is an **open protocol** maintained at [agentclientprotocol.com](https://agentclientprotocol.com/). Cursor **implements** an ACP server via the CLI: `agent acp`. Cursor adds **Cursor-specific authentication and extension RPCs** (for example `cursor_login`, `cursor/ask_question`), so the framing is shared, while those extensions are Cursor-specific.

### Summary

ACP lets a **custom client** spawn `agent acp` and control sessions over **stdio** using **JSON-RPC 2.0** with **newline-delimited JSON** (one message per line).

- Client writes requests/notifications to **stdin**.
- Server writes responses/notifications to **stdout**.
- **stderr** may contain logs.

### Typical session flow

1. `initialize`
2. `authenticate` with `methodId: "cursor_login"`
3. `session/new` or `session/load`
4. `session/prompt`
5. Handle `session/update` while the model streams
6. Handle `session/request_permission` with `allow-once`, `allow-always`, or `reject-once`
7. Optionally `session/cancel`

If permission requests are not answered, tool execution can block.

### Authentication

Advertised method: `cursor_login`. Pre-authenticate when possible:

```bash
agent login
```

```bash
agent --api-key "$CURSOR_API_KEY" acp
```

Optional endpoint / TLS flags on the root command (see ACP docs).

### Sessions, modes, permissions

- **Modes**: same core modes as CLI — `agent`, `plan`, `ask`.
- **Permissions**: respond to `session/request_permission` with the outcomes above.

### MCP with ACP

ACP can use MCP servers from **project-level or user-level** `.cursor/mcp.json`. **Team-level** MCP servers from the Cursor dashboard are **not** supported in ACP mode (per docs).

### Cursor extension methods (examples)

| Method | Use |
| --- | --- |
| `cursor/ask_question` | Multiple-choice user questions |
| `cursor/create_plan` | Plan approval |
| `cursor/update_todos` | Todo state for UI |
| `cursor/task` | Subagent task completion |
| `cursor/generate_image` | Generated image output |

### Example snippets

Start ACP server:

```bash
agent acp
```

With API key:

```bash
agent --api-key "$CURSOR_API_KEY" acp
```

Conceptual message shape (single line per message):

```json
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":1,"clientCapabilities":{"fs":{"readTextFile":false,"writeTextFile":false},"terminal":false},"clientInfo":{"name":"my-client","version":"0.1.0"}}}
```

The official docs include a **minimal Node.js client** that spawns `agent acp`, sends RPCs, prints streaming chunks from `session/update`, and auto-approves permissions with `allow-once`.

### Use cases

1. **Editor integrations** (JetBrains, Neovim via avante.nvim, Zed, custom editors) that want Cursor’s agent with a native UI.
2. **Orchestrators** that need structured control: permissions, session lifecycle, cancel, and streaming — beyond a single `agent -p` invocation.
3. **Internal tools** that embed the agent as a child process and treat it as a service.

---

## 3. Cloud Agents API

### Summary

**HTTP REST API** at `https://api.cursor.com` to **launch and manage cloud agents** that work on **GitHub repositories**: create agents, poll status, read conversations, list/download artifacts, send follow-ups, stop or delete agents.

Documented as **beta** and listed as available on **all plans** in the [API overview](https://cursor.com/docs/api). Full schemas: [OpenAPI YAML](https://cursor.com/docs-static/cloud-agents-openapi.yaml).

### Authentication

- **Basic authentication**: API key as **username**, **empty password** (or equivalent `Authorization` header).
- Create keys from **Cursor Dashboard → Cloud Agents** (per endpoints documentation).

Example:

```bash
curl https://api.cursor.com/v0/me -u YOUR_API_KEY:
```

### Important constraints (from docs)

- **MCP is not supported** by the Cloud Agents API yet.
- **Artifacts**: list/download applies to agents created within the **last 6 months**; older agents may return `400`.
- **`GET /v0/repositories`**: very strict limits (documented as **1 request per user per minute**, **30 per user per hour**); responses can be slow for users with many repos — handle absence gracefully.

### Core endpoints (overview)

| Operation | Method | Path |
| --- | --- | --- |
| List agents | GET | `/v0/agents` |
| Launch agent | POST | `/v0/agents` |
| Agent status | GET | `/v0/agents/{id}` |
| Conversation | GET | `/v0/agents/{id}/conversation` |
| Artifacts | GET | `/v0/agents/{id}/artifacts` |
| Download artifact | GET | `/v0/agents/{id}/artifacts/download?path=...` |
| Follow-up | POST | `/v0/agents/{id}/followup` |
| Stop | POST | `/v0/agents/{id}/stop` |
| Delete | DELETE | `/v0/agents/{id}` |
| API key info | GET | `/v0/me` |
| List models | GET | `/v0/models` |
| List GitHub repos | GET | `/v0/repositories` |

Webhook configuration for status changes is described in [Cloud Agent webhooks](https://cursor.com/docs/cloud-agent/api/webhooks).

### Launch payload (conceptual)

Required:

- `prompt.text` — task instructions (optional `prompt.images` with base64, max 5).
- `source` — `repository` URL and optional `ref`, or `source.prUrl` to work from an existing PR.

Optional:

- `model` — explicit model id, or `"default"` / omit for defaults.
- `target` — `autoCreatePr`, `branchName`, `openAsCursorGithubApp`, `skipReviewerRequest`, `autoBranch` (when using `prUrl`), etc.
- `webhook` — URL and optional secret for notifications.

### Example snippets

List agents:

```bash
curl --request GET \
  --url https://api.cursor.com/v0/agents \
  -u YOUR_API_KEY:
```

Launch an agent:

```bash
curl --request POST \
  --url https://api.cursor.com/v0/agents \
  -u YOUR_API_KEY: \
  --header 'Content-Type: application/json' \
  --data '{
    "prompt": { "text": "Add a README with install steps" },
    "source": {
      "repository": "https://github.com/your-org/your-repo",
      "ref": "main"
    },
    "target": {
      "autoCreatePr": true,
      "branchName": "feature/add-readme"
    }
  }'
```

Poll status:

```bash
curl --request GET \
  --url https://api.cursor.com/v0/agents/bc_abc123 \
  -u YOUR_API_KEY:
```

Follow-up:

```bash
curl --request POST \
  --url https://api.cursor.com/v0/agents/bc_abc123/followup \
  -u YOUR_API_KEY: \
  --header 'Content-Type: application/json' \
  --data '{"prompt":{"text":"Also add a troubleshooting section"}}'
```

List models (for the `model` field):

```bash
curl --request GET \
  --url https://api.cursor.com/v0/models \
  -u YOUR_API_KEY:
```

### Use cases

1. **CI or backend jobs** that enqueue work, poll until `FINISHED`, then surface `summary` and PR URL.
2. **Ticketing integrations** (create agent from ticket, post agent URL or PR back).
3. **Scheduled automation** across many repos with optional **webhooks** instead of tight polling.
4. **Audit / analytics** via conversation and artifact retrieval (within retention limits).

### Rate limits

Cloud Agents API uses standard per-team rate limiting (see [API overview](https://cursor.com/docs/api)). Artifact endpoints have additional documented limits (for example 300/min and 6000/hour for list/download in the endpoints doc). Handle `429` with backoff.

---

## 4. Comparison (when to use what)

| Aspect | **CLI (`agent`, `-p`)** | **ACP (`agent acp`)** | **Cloud Agents API** |
| --- | --- | --- | --- |
| Transport | Shell / subprocess | stdio JSON-RPC | HTTPS REST |
| Where work runs | Local machine | Local process (agent) | Cursor cloud on repo |
| Strength | Simplest scripting | Rich session + permissions + streaming | Remote, PR-centric, org automation |
| Auth | Login / API key / token (see CLI docs) | Same + `cursor_login` flow | Dashboard API key (Basic) |
| MCP | Supported (CLI) | Project/user MCP; not team dashboard MCP | Not supported yet |

---

## 5. Other Cursor APIs (context only)

The [API overview](https://cursor.com/docs/api) also lists **Admin**, **Analytics**, and **AI Code Tracking** APIs (enterprise-oriented) for team management, usage metrics, and attribution. These are separate from ACP and from the Cloud Agents product API.
