# Security Policy

## Supported versions

pg_protoexport is an actively developed tool. Security fixes are applied to the
latest released version. Please make sure you are on the most recent
[release](https://github.com/xfischer/pg_protoexport/releases) or NuGet package
before reporting an issue.

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, report them privately via GitHub's
[Report a vulnerability](https://github.com/xfischer/pg_protoexport/security/advisories/new)
feature (Security → Advisories), or by emailing the maintainer at
**xavier.fischer@enterprisedb.com**.

Please include:

- a description of the issue and its impact,
- steps to reproduce (a minimal `.pcap`/`.pcapng` sample is ideal),
- the version of pg_protoexport affected.

You can expect an acknowledgement within a few business days. We will keep you
informed as we investigate and work on a fix, and will coordinate disclosure with you.

## Scope

pg_protoexport parses **untrusted capture files** (`.pcap`/`.pcapng`) and records
live network traffic. Parser-level issues are in scope, including:

- crashes, hangs, or unbounded memory/CPU use triggered by a crafted capture,
- out-of-bounds reads or other memory-safety problems in the wire-protocol parser,
- path-traversal or unsafe file writes in the export pipeline.

Capturing requires native pcap libraries (Npcap/libpcap); vulnerabilities in those
dependencies should be reported to their respective projects.
