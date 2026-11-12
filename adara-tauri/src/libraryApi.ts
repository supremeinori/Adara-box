import { invoke } from "@tauri-apps/api/core";

export type BookFileType = "pdf" | "epub";

export interface LibraryBookDto {
  id: string;
  title: string;
  author: string;
  category: string;
  status: string;
  notes: string;
  fileType: BookFileType;
  fileName: string;
  fileSize: number;
  hasCover: boolean;
  currentPage: number;
  totalPages: number;
  currentCfi?: string | null;
  readingProgress: number;
  lastReadAt?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface UpdateBookInput {
  id: string;
  title: string;
  author: string;
  category: string;
  status: string;
  notes: string;
  coverDataUrl?: string | null;
  clearCover?: boolean;
}

export interface BookProgressInput {
  id: string;
  currentPage?: number;
  totalPages?: number;
  currentCfi?: string;
  readingProgress?: number;
  status?: string;
}

const coverCache = new Map<string, string | null>();

export const BOOK_CATEGORIES = [
  "Any",
  "Psychology",
  "Philosophy",
  "Literature",
  "Cinema",
  "Technology",
  "Other",
] as const;

export const BOOK_STATUSES = [
  "New",
  "Want to Read",
  "Reading",
  "Finished",
] as const;

export function libListBooks(): Promise<LibraryBookDto[]> {
  return invoke("lib_list_books");
}

export function libAddBookFromPath(args: {
  title: string;
  author: string;
  category: string;
  status: string;
  notes: string;
  sourcePath: string;
  coverDataUrl?: string | null;
}): Promise<LibraryBookDto> {
  return invoke("lib_add_book_from_path", {
    title: args.title,
    author: args.author,
    category: args.category,
    status: args.status,
    notes: args.notes,
    sourcePath: args.sourcePath,
    coverDataUrl: args.coverDataUrl ?? null,
  });
}

export function libAddBookBytes(args: {
  title: string;
  author: string;
  category: string;
  status: string;
  notes: string;
  fileName: string;
  bytes: Uint8Array;
  coverDataUrl?: string | null;
}): Promise<LibraryBookDto> {
  return invoke("lib_add_book_bytes", {
    title: args.title,
    author: args.author,
    category: args.category,
    status: args.status,
    notes: args.notes,
    fileName: args.fileName,
    bytes: Array.from(args.bytes),
    coverDataUrl: args.coverDataUrl ?? null,
  });
}

export function libUpdateBook(input: UpdateBookInput): Promise<LibraryBookDto> {
  coverCache.delete(input.id);
  return invoke("lib_update_book", { input });
}

export function libSaveProgress(input: BookProgressInput): Promise<LibraryBookDto> {
  return invoke("lib_save_progress", { input });
}

export function libDeleteBook(id: string): Promise<void> {
  coverCache.delete(id);
  return invoke("lib_delete_book", { id });
}

export async function libGetCoverDataUrl(id: string): Promise<string | null> {
  if (coverCache.has(id)) {
    return coverCache.get(id) ?? null;
  }
  const url = await invoke<string | null>("lib_get_cover_data_url", { id }).catch(() => null);
  coverCache.set(id, url);
  return url;
}

export function invalidateCoverCache(id: string) {
  coverCache.delete(id);
}

export function libGetBookBytes(id: string): Promise<ArrayBuffer> {
  return invoke<ArrayBuffer>("lib_get_book_bytes", { id });
}

export async function libPickBookFile(): Promise<{ path: string; name: string; size: number } | null> {
  const res = await invoke<[string, string, number] | null>("lib_pick_book_file");
  if (!res) return null;
  return { path: res[0], name: res[1], size: res[2] };
}

export function libPickCoverImage(): Promise<string | null> {
  return invoke("lib_pick_cover_image");
}

export function openWorkspaceWindow(): Promise<void> {
  return invoke("open_workspace_window");
}

export function formatBookTitle(raw: string): string {
  const clean = raw.replace(/\.(pdf|epub)$/i, "").replace(/[_-]+/g, " ").trim();
  return clean || "Untitled Book";
}

export function formatBytes(bytes: number): string {
  if (!bytes || bytes <= 0) return "0 B";
  const units = ["B", "KB", "MB", "GB"];
  let i = 0;
  let v = bytes;
  while (v >= 1024 && i < units.length - 1) {
    v /= 1024;
    i++;
  }
  return `${v >= 10 || i === 0 ? Math.round(v) : v.toFixed(1)} ${units[i]}`;
}
