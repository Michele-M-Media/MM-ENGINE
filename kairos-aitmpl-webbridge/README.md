# Kairos Forge Native AITMPL MCP v0.8

Source v0.8 is stored as an archive at `kairos-aitmpl-webbridge/bridge-v0.1.tar.gz` for compatibility with the EXISTING Render build configuration. The production MCP endpoint is HTTPS `/mcp`.

This public service has 14 read-only tools: 10 catalog queries plus automatic skill routing/preparation, in-memory static syntax checking, and diff preview. No shell/process execution, secrets, or user-project file access is permitted. Private PC control requires a separate authenticated MCP.

The AITMPL registry indexes all 8 catalog categories. A component is not automatically executed by being indexed. Third-party text remains untrusted and license restrictions apply.
