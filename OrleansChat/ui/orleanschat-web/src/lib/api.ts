import type { ChatMessage, NotificationItem } from "./types";

function resolveBaseUrl(): string {
  const configured = process.env.NEXT_PUBLIC_API_BASE_URL?.replace(/\/$/, "");
  if (configured) {
    return configured;
  }

  return "";
}

const baseUrl = resolveBaseUrl();

type AnyObject = Record<string, unknown>;

function createConnectionId(): string {
  const cryptoObj = (globalThis as unknown as { crypto?: Crypto }).crypto;
  if (cryptoObj?.randomUUID) {
    return cryptoObj.randomUUID();
  }

  return "xxxxxxxxxxxx4xxxyxxxxxxxxxxxxxxx".replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === "x" ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

export function normalizeChatMessage(raw: AnyObject): ChatMessage {
  const messageId =
    (raw["messageId"] as string | undefined) ??
    (raw["MessageId"] as string | undefined);
  const roomId =
    (raw["roomId"] as string | undefined) ??
    (raw["RoomId"] as string | undefined) ??
    "";
  const userId =
    (raw["userId"] as string | undefined) ??
    (raw["UserId"] as string | undefined) ??
    "";
  const text =
    (raw["text"] as string | undefined) ??
    (raw["Text"] as string | undefined) ??
    "";
  const timestampUtc =
    (raw["timestampUtc"] as string | undefined) ??
    (raw["TimestampUtc"] as string | undefined);

  return { messageId, roomId, userId, text, timestampUtc };
}

export function normalizeNotification(raw: AnyObject): NotificationItem {
  return {
    notificationId:
      ((raw["notificationId"] as string | undefined) ??
        (raw["NotificationId"] as string | undefined) ??
        "") as string,
    userId:
      ((raw["userId"] as string | undefined) ??
        (raw["UserId"] as string | undefined) ??
        "") as string,
    type:
      ((raw["type"] as string | undefined) ??
        (raw["Type"] as string | undefined) ??
        "") as string,
    payload:
      ((raw["payload"] as string | undefined) ??
        (raw["Payload"] as string | undefined) ??
        "") as string,
    read:
      ((raw["read"] as boolean | undefined) ??
        (raw["Read"] as boolean | undefined) ??
        false) as boolean,
    createdUtc:
      ((raw["createdUtc"] as string | undefined) ??
        (raw["CreatedUtc"] as string | undefined) ??
        "") as string,
  };
}

export async function connectUser(userId: string): Promise<void> {
  await fetch(`${baseUrl}/api/users/${userId}/connect`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ connectionId: createConnectionId() }),
  });
}

export async function joinRoom(roomId: string, userId: string): Promise<void> {
  await fetch(`${baseUrl}/api/rooms/${roomId}/join`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ userId }),
  });
}

export async function sendMessage(
  roomId: string,
  userId: string,
  text: string,
): Promise<void> {
  await fetch(`${baseUrl}/api/rooms/${roomId}/messages`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ userId, text }),
  });
}

export async function getHistory(roomId: string): Promise<ChatMessage[]> {
  const response = await fetch(`${baseUrl}/api/rooms/${roomId}/history`);
  const raw = (await response.json()) as AnyObject[];
  return raw.map(normalizeChatMessage);
}

export async function getNotifications(userId: string): Promise<NotificationItem[]> {
  const response = await fetch(`${baseUrl}/api/users/${userId}/notifications`);
  const raw = (await response.json()) as AnyObject[];
  return raw.map(normalizeNotification);
}

export function streamUrl(roomId: string): string {
  return `${baseUrl}/api/rooms/${roomId}/stream`;
}
