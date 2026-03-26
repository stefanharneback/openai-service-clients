import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  getHealth,
  sendLlm,
  streamLlm,
  postWhisper,
  getUsage,
  getAdminUsage,
  parseSseDataLine,
  extractDisplayChunk,
} from "./client.js";

const mockFetch = vi.fn<typeof globalThis.fetch>();
vi.stubGlobal("fetch", mockFetch);

const jsonResponse = (body: unknown, status = 200): Response =>
  new Response(JSON.stringify(body), {
    status,
    headers: { "content-type": "application/json" },
  });

const textResponse = (body: string, status = 200): Response => new Response(body, { status });

beforeEach(() => {
  mockFetch.mockReset();
});

// ---------- SSE helpers ----------

describe("parseSseDataLine", () => {
  it("extracts data from a valid SSE line", () => {
    expect(parseSseDataLine("data: hello")).toBe("hello");
  });

  it("trims whitespace from the value", () => {
    expect(parseSseDataLine("data:   spaced  ")).toBe("spaced");
  });

  it("returns null for non-data lines", () => {
    expect(parseSseDataLine("event: update")).toBeNull();
    expect(parseSseDataLine("")).toBeNull();
    expect(parseSseDataLine(": comment")).toBeNull();
  });

  it("handles data: with no value", () => {
    expect(parseSseDataLine("data:")).toBe("");
  });
});

describe("extractDisplayChunk", () => {
  it("returns done marker for [DONE]", () => {
    expect(extractDisplayChunk("[DONE]")).toBe("\n\n[done]");
  });

  it("extracts delta text from JSON payload", () => {
    const payload = JSON.stringify({ type: "response.output_text.delta", delta: "Hi" });
    expect(extractDisplayChunk(payload)).toBe("Hi");
  });

  it("returns completed marker for response.completed", () => {
    const payload = JSON.stringify({ type: "response.completed" });
    expect(extractDisplayChunk(payload)).toBe("\n\n[completed]");
  });

  it("returns empty string for JSON without delta or completed", () => {
    const payload = JSON.stringify({ type: "response.created" });
    expect(extractDisplayChunk(payload)).toBe("");
  });

  it("returns raw data with newline for unparseable input", () => {
    expect(extractDisplayChunk("raw text")).toBe("raw text\n");
  });
});

// ---------- getHealth ----------

describe("getHealth", () => {
  it("returns parsed health response", async () => {
    const body = { ok: true, service: "oais", requestId: "r-1", now: "2026-01-01T00:00:00Z" };
    mockFetch.mockResolvedValue(jsonResponse(body));

    const result = await getHealth("http://localhost:3000");
    expect(result).toEqual(body);
    expect(mockFetch).toHaveBeenCalledWith("http://localhost:3000/health");
  });

  it("throws on non-OK response", async () => {
    mockFetch.mockResolvedValue(textResponse("down", 503));
    await expect(getHealth("http://localhost:3000")).rejects.toThrow("status 503");
  });
});

// ---------- sendLlm ----------

describe("sendLlm", () => {
  it("sends JSON payload with auth header", async () => {
    const body = { id: "resp-1", output: [{ content: [{ text: "hello" }] }] };
    mockFetch.mockResolvedValue(jsonResponse(body));

    const result = await sendLlm("http://localhost:3000", "sk-test", {
      model: "gpt-5.4-mini",
      input: "Hi",
    });

    expect(result).toEqual(body);
    const [url, init] = mockFetch.mock.calls[0];
    expect(url).toBe("http://localhost:3000/v1/llm");
    expect((init as RequestInit).headers).toEqual(
      expect.objectContaining({ authorization: "Bearer sk-test" }),
    );
  });

  it("throws with status and body on failure", async () => {
    mockFetch.mockResolvedValue(textResponse('{"error":"bad"}', 400));
    await expect(
      sendLlm("http://localhost:3000", "sk-test", { model: "gpt-5.4", input: "x" }),
    ).rejects.toThrow("400");
  });

  it("returns raw text when response is not JSON", async () => {
    mockFetch.mockResolvedValue(textResponse("plain answer"));
    const result = await sendLlm("http://localhost:3000", "sk-test", {
      model: "gpt-5.4",
      input: "x",
    });
    expect(result).toBe("plain answer");
  });
});

// ---------- streamLlm ----------

const sseStream = (lines: string[]): Response => {
  const text = lines.join("\n") + "\n";
  const stream = new ReadableStream<Uint8Array>({
    start(controller) {
      controller.enqueue(new TextEncoder().encode(text));
      controller.close();
    },
  });
  return new Response(stream, { status: 200 });
};

describe("streamLlm", () => {
  it("delivers parsed delta chunks to callback", async () => {
    const chunks: string[] = [];
    const body = [
      `data: ${JSON.stringify({ type: "response.output_text.delta", delta: "Hello" })}`,
      `data: ${JSON.stringify({ type: "response.output_text.delta", delta: " world" })}`,
      "data: [DONE]",
    ];
    mockFetch.mockResolvedValue(sseStream(body));

    await streamLlm("http://localhost:3000", "sk-test", { model: "gpt-5.4", input: "Hi" }, (c) =>
      chunks.push(c),
    );

    expect(chunks).toEqual(["Hello", " world", "\n\n[done]"]);
  });

  it("throws on non-OK response", async () => {
    mockFetch.mockResolvedValue(textResponse("unauthorized", 401));
    await expect(
      streamLlm("http://localhost:3000", "sk-bad", { model: "gpt-5.4", input: "x" }, () => {}),
    ).rejects.toThrow("401");
  });

  it("throws when body is null", async () => {
    // Simulate a response with no body
    const resp = new Response(null, { status: 200 });
    Object.defineProperty(resp, "body", { value: null });
    mockFetch.mockResolvedValue(resp);
    await expect(
      streamLlm("http://localhost:3000", "sk-test", { model: "gpt-5.4", input: "x" }, () => {}),
    ).rejects.toThrow("empty");
  });

  it("ignores non-data SSE lines", async () => {
    const chunks: string[] = [];
    const body = [
      "event: ping",
      ": comment",
      `data: ${JSON.stringify({ type: "response.output_text.delta", delta: "ok" })}`,
      "data: [DONE]",
    ];
    mockFetch.mockResolvedValue(sseStream(body));

    await streamLlm("http://localhost:3000", "sk-test", { model: "gpt-5.4", input: "x" }, (c) =>
      chunks.push(c),
    );

    expect(chunks).toEqual(["ok", "\n\n[done]"]);
  });
});

// ---------- postWhisper ----------

describe("postWhisper", () => {
  it("sends multipart form data with file", async () => {
    const body = { text: "Hello world" };
    mockFetch.mockResolvedValue(jsonResponse(body));

    const file = new File(["audio"], "test.wav", { type: "audio/wav" });
    const result = await postWhisper("http://localhost:3000", "sk-test", "gpt-4o-transcribe", file);

    expect(result).toEqual(body);
    const [url, init] = mockFetch.mock.calls[0];
    expect(url).toBe("http://localhost:3000/v1/whisper");
    expect((init as RequestInit).body).toBeInstanceOf(FormData);
  });

  it("throws on failure", async () => {
    mockFetch.mockResolvedValue(textResponse("too large", 413));
    const file = new File(["x"], "big.wav");
    await expect(
      postWhisper("http://localhost:3000", "sk-test", "gpt-4o-transcribe", file),
    ).rejects.toThrow("413");
  });
});

// ---------- getUsage ----------

describe("getUsage", () => {
  it("fetches usage with pagination params", async () => {
    const body = { clientId: "c1", items: [], limit: 10, offset: 0 };
    mockFetch.mockResolvedValue(jsonResponse(body));

    const result = await getUsage("http://localhost:3000", "sk-test", 10, 0);
    expect(result).toEqual(body);
    expect(mockFetch).toHaveBeenCalledWith("http://localhost:3000/v1/usage?limit=10&offset=0", {
      headers: { authorization: "Bearer sk-test" },
    });
  });

  it("throws on failure", async () => {
    mockFetch.mockResolvedValue(textResponse("unauthorized", 401));
    await expect(getUsage("http://localhost:3000", "sk-bad")).rejects.toThrow("401");
  });
});

// ---------- getAdminUsage ----------

describe("getAdminUsage", () => {
  it("fetches admin usage with pagination", async () => {
    const body = { items: [{ id: "u-1" }], limit: 5, offset: 0 };
    mockFetch.mockResolvedValue(jsonResponse(body));

    const result = await getAdminUsage("http://localhost:3000", "admin-key", 5, 0);
    expect(result).toEqual(body);
    expect(mockFetch).toHaveBeenCalledWith(
      "http://localhost:3000/v1/admin/usage?limit=5&offset=0",
      { headers: { authorization: "Bearer admin-key" } },
    );
  });

  it("throws on failure", async () => {
    mockFetch.mockResolvedValue(textResponse("forbidden", 403));
    await expect(getAdminUsage("http://localhost:3000", "bad-key")).rejects.toThrow("403");
  });
});
