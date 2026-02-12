## Summary

<!-- Briefly describe what this PR does and why it's needed -->

## What changed

<!-- List the key changes made in this PR:
- Added/Modified/Removed X
- Implemented Y feature
- Fixed Z bug
-->

## Why

<!-- Explain the rationale behind these changes:
- What problem does this solve?
- Why was this approach chosen?
- How does it align with the project architecture?
-->

## Testing

<!-- Optional: Describe how you tested these changes:
- Manual testing via Swagger
- Unit tests added/updated
- Docker Compose verification
- Service integration tests
-->

## Checklist

- [ ] Changes follow the architecture patterns in `.github/copilot-instructions.md`
- [ ] Minimal API endpoints (no controllers)
- [ ] Interfaces registered in DI container
- [ ] Service specs and bounded contexts respected
- [ ] Unit tests added/updated (if applicable)
- [ ] Swagger documentation includes `.WithTags()`, `.WithName()`, `.WithOpenApi()`
- [ ] Graceful degradation for upstream service failures (if applicable)
- [ ] One type per file (except related DTOs)
- [ ] Conventional commit message format (`feat:`, `fix:`, `test:`, `docs:`)

## Related

<!-- Optional: Link to related issues, docs, or specs -->
<!-- - Fixes #123 -->
<!-- - Related to docs/service-name.md -->
