#!/usr/bin/env node
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { createServer } from './server.js';

// stdout carries the protocol, so anything human-readable must go to stderr.
const server = createServer();
await server.connect(new StdioServerTransport());
console.error('courier-mcp-server running on stdio');
