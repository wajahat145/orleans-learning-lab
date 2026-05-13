export const runtime = "nodejs";
export const dynamic = "force-dynamic";

export async function GET(
  _request: Request,
  context: { params: { roomId: string } },
) {
  const roomId = context.params.roomId;
  const upstream = await fetch(
    `http://localhost:5000/api/rooms/${encodeURIComponent(roomId)}/stream`,
    {
      headers: {
        Accept: "text/event-stream",
        "Cache-Control": "no-cache",
        Connection: "keep-alive",
      },
    },
  );

  if (!upstream.body) {
    return new Response("Upstream stream was empty", { status: 502 });
  }

  const reader = upstream.body.getReader();
  const stream = new ReadableStream<Uint8Array>({
    async start(controller) {
      try {
        while (true) {
          const { value, done } = await reader.read();
          if (done) {
            break;
          }
          if (value) {
            controller.enqueue(value);
          }
        }
      } catch {
      } finally {
        controller.close();
        try {
          reader.releaseLock();
        } catch {
        }
      }
    },
    async cancel() {
      try {
        await reader.cancel();
      } catch {
      }
    },
  });

  const headers = new Headers(upstream.headers);
  headers.set("Content-Type", "text/event-stream");
  headers.set("Cache-Control", "no-cache");
  headers.set("Connection", "keep-alive");
  headers.set("X-Accel-Buffering", "no");

  return new Response(stream, {
    status: upstream.status,
    headers,
  });
}
