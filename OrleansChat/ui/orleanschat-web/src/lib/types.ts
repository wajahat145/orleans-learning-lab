export type ChatMessage = {
  messageId?: string;
  roomId: string;
  userId: string;
  text: string;
  timestampUtc?: string;
};

export type NotificationItem = {
  notificationId: string;
  userId: string;
  type: string;
  payload: string;
  read: boolean;
  createdUtc: string;
};
