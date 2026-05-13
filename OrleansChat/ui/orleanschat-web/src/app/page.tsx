"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import {
  connectUser,
  getHistory,
  getNotifications,
  joinRoom,
  normalizeChatMessage,
  sendMessage,
  streamUrl,
} from "@/lib/api";
import type { ChatMessage, NotificationItem } from "@/lib/types";

export default function Home() {
  const [roomId, setRoomId] = useState("general");
  const [userId, setUserId] = useState("user-a");
  const [text, setText] = useState("");
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [status, setStatus] = useState("Ready");

  const title = useMemo(() => `Room: ${roomId} | User: ${userId}`, [roomId, userId]);

  useEffect(() => {
    let stream: EventSource | undefined;
    let pollTimer: ReturnType<typeof setInterval> | undefined;
    const run = async () => {
      setStatus("Connecting...");
      await connectUser(userId);
      await joinRoom(roomId, userId);
      const history = await getHistory(roomId);
      setMessages(history);
      setNotifications(await getNotifications(userId));
      setStatus("Connected");

      stream = new EventSource(streamUrl(roomId));
      stream.onopen = () => {
        setStatus("Live connected");
      };
      stream.onmessage = (event) => {
        const message = normalizeChatMessage(JSON.parse(event.data));
        setMessages((prev) => {
          if (message.messageId && prev.some((x) => x.messageId === message.messageId)) {
            return prev;
          }

          return [message, ...prev];
        });
      };
      stream.onerror = () => {
        setStatus("Live disconnected (polling)");
      };

      pollTimer = setInterval(async () => {
        try {
          const latest = await getHistory(roomId);
          setMessages((prev) => {
            const seen = new Set(prev.map((x) => x.messageId).filter(Boolean) as string[]);
            const merged: ChatMessage[] = [];

            for (const m of latest) {
              if (m.messageId && seen.has(m.messageId)) {
                continue;
              }
              merged.push(m);
            }

            if (merged.length === 0) {
              return prev;
            }

            return [...merged, ...prev].slice(0, 200);
          });
        } catch {
        }
      }, 3000);
    };

    void run();

    return () => {
      stream?.close();
      if (pollTimer) {
        clearInterval(pollTimer);
      }
    };
  }, [roomId, userId]);

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    if (!text.trim()) {
      return;
    }

    await sendMessage(roomId, userId, text.trim());
    setText("");
  };

  return (
    <main className="mx-auto flex min-h-screen w-full max-w-6xl flex-col gap-4 bg-white p-6 text-slate-900">
      <h1 className="text-2xl font-bold">OrleansChat UI</h1>
      <p className="text-sm text-slate-600">{title}</p>
      <p className="text-sm text-emerald-700">{status}</p>

      <section className="grid gap-4 rounded-xl border border-slate-200 p-4 md:grid-cols-3">
        <label className="flex flex-col gap-2 text-sm">
          User ID
          <input
            className="rounded border border-slate-300 px-3 py-2"
            value={userId}
            onChange={(e) => setUserId(e.target.value)}
          />
        </label>
        <label className="flex flex-col gap-2 text-sm">
          Room ID
          <input
            className="rounded border border-slate-300 px-3 py-2"
            value={roomId}
            onChange={(e) => setRoomId(e.target.value)}
          />
        </label>
        <form className="flex items-end gap-2" onSubmit={onSubmit}>
          <input
            className="w-full rounded border border-slate-300 px-3 py-2"
            placeholder="Type a message"
            value={text}
            onChange={(e) => setText(e.target.value)}
          />
          <button className="rounded bg-slate-900 px-4 py-2 text-white" type="submit">
            Send
          </button>
        </form>
      </section>

      <section className="grid flex-1 gap-4 md:grid-cols-3">
        <div className="md:col-span-2 rounded-xl border border-slate-200 p-4">
          <h2 className="mb-3 text-lg font-semibold">Live Messages</h2>
          <div className="flex max-h-[60vh] flex-col gap-2 overflow-auto">
            {messages.map((message) => (
              <div key={`${message.messageId ?? "m"}-${message.timestampUtc}`} className="rounded border border-slate-100 bg-slate-50 p-3">
                <p className="text-sm font-semibold">{message.userId}</p>
                <p className="text-sm">{message.text}</p>
              </div>
            ))}
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 p-4">
          <h2 className="mb-3 text-lg font-semibold">Notifications</h2>
          <div className="flex max-h-[60vh] flex-col gap-2 overflow-auto">
            {notifications.length === 0 && <p className="text-sm text-slate-500">No unread notifications</p>}
            {notifications.map((notification) => (
              <div key={notification.notificationId} className="rounded border border-slate-100 bg-amber-50 p-3 text-sm">
                {notification.payload}
              </div>
            ))}
          </div>
        </div>
      </section>
    </main>
  );
}
