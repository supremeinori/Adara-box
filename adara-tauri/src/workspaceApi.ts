import { invoke } from "@tauri-apps/api/core";
import { emit, listen } from "@tauri-apps/api/event";

export type ResourceType = "application" | "file" | "folder" | "link";

export interface WorkspaceDto {
  id: string;
  name: string;
  icon: string;
  sortOrder: number;
  createdAt: string;
  updatedAt: string;
  resourceCount: number;
}

export interface ResourceDto {
  id: string;
  workspaceId: string;
  name: string;
  type: ResourceType;
  target: string;
  sortOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface AvailabilityResult {
  resourceId: string;
  available: boolean;
  reason?: string | null;
}

export interface LaunchItemResult {
  resourceId: string;
  name: string;
  type: ResourceType;
  target: string;
  success: boolean;
  errorCode?: string | null;
  errorMessage?: string | null;
}

export interface BatchLaunchSummary {
  workspaceId: string;
  total: number;
  launched: number;
  failed: number;
  cancelled: boolean;
  results: LaunchItemResult[];
}

export interface LaunchProgressEvent {
  workspaceId: string;
  completed: number;
  total: number;
  currentName: string;
  cancelled: boolean;
}

export interface NewResourceInput {
  name?: string;
  type?: ResourceType;
  target: string;
}

export const WORKSPACE_CHANGED_EVENT = "workspace-data-changed";

const shellIconCache = new Map<string, string | null>();
const customIconCache = new Map<string, string | null>();

export function extractErrorMessage(err: unknown): string {
  if (typeof err === "string") return err;
  if (err && typeof err === "object" && "message" in err) {
    return String((err as { message: unknown }).message);
  }
  return "Unexpected error";
}

export function formatDomainOrTarget(resource: ResourceDto): string {
  if (resource.type === "link") {
    try {
      const url = new URL(resource.target);
      return url.hostname.replace(/^www\./i, "");
    } catch {
      return resource.target;
    }
  }
  const parts = resource.target.split(/[\\/]/).filter(Boolean);
  if (parts.length >= 2) {
    return `${parts[parts.length - 2]}\\${parts[parts.length - 1]}`;
  }
  return resource.target;
}

export async function notifyWorkspaceChanged(workspaceId?: string) {
  await emit(WORKSPACE_CHANGED_EVENT, { workspaceId: workspaceId ?? null }).catch(() => {});
}

export function listenWorkspaceChanged(cb: (workspaceId: string | null) => void): Promise<() => void> {
  return listen<{ workspaceId?: string | null }>(WORKSPACE_CHANGED_EVENT, (e) => {
    cb(e.payload?.workspaceId ?? null);
  });
}

export const wsList = () => invoke<WorkspaceDto[]>("ws_list");

export const wsCreate = async (name: string, icon?: string | null) => {
  const created = await invoke<WorkspaceDto>("ws_create", { name, icon: icon ?? null });
  await notifyWorkspaceChanged(created.id);
  return created;
};

export const wsUpdate = async (id: string, name: string, icon?: string | null) => {
  const updated = await invoke<WorkspaceDto>("ws_update", { id, name, icon: icon ?? null });
  await notifyWorkspaceChanged(updated.id);
  return updated;
};

export const wsReorder = async (orderedIds: string[]) => {
  await invoke<void>("ws_reorder", { orderedIds });
  await notifyWorkspaceChanged();
};

export const wsDelete = async (id: string) => {
  await invoke<void>("ws_delete", { id });
  await notifyWorkspaceChanged();
};

export const wsCreateBackup = () => invoke<string>("ws_create_backup");
export const wsPickIconImage = () => invoke<string | null>("ws_pick_icon_image");

export function isImageWorkspaceIcon(icon: string | null | undefined): boolean {
  if (!icon) return false;
  return icon.startsWith("custom:") || /[\\/]/.test(icon) || /^[a-zA-Z]:/.test(icon);
}

export async function wsGetIconDataUrl(icon: string): Promise<string | null> {
  if (!isImageWorkspaceIcon(icon)) return null;
  if (customIconCache.has(icon)) return customIconCache.get(icon) ?? null;
  try {
    const dataUrl = await invoke<string | null>("ws_get_icon_data_url", { icon });
    customIconCache.set(icon, dataUrl ?? null);
    return dataUrl ?? null;
  } catch {
    return null;
  }
}

export const resList = (workspaceId: string) =>
  invoke<ResourceDto[]>("res_list", { workspaceId });

export const resAdd = async (
  workspaceId: string,
  name: string,
  resourceType: ResourceType,
  target: string
) => {
  const added = await invoke<ResourceDto>("res_add", {
    workspaceId,
    name,
    resourceType,
    target,
  });
  await notifyWorkspaceChanged(workspaceId);
  return added;
};

export const resAddMany = async (workspaceId: string, items: NewResourceInput[]) => {
  const added = await invoke<ResourceDto[]>("res_add_many", { workspaceId, items });
  if (added.length > 0) await notifyWorkspaceChanged(workspaceId);
  return added;
};

export const resUpdate = async (
  id: string,
  workspaceId: string,
  name: string,
  resourceType: ResourceType,
  target: string
) => {
  const updated = await invoke<ResourceDto>("res_update", {
    id,
    name,
    resourceType,
    target,
  });
  await notifyWorkspaceChanged(workspaceId);
  return updated;
};

export const resRemove = async (id: string, workspaceId: string) => {
  await invoke<void>("res_remove", { id });
  await notifyWorkspaceChanged(workspaceId);
};

export const resReorder = async (workspaceId: string, orderedIds: string[]) => {
  await invoke<void>("res_reorder", { workspaceId, orderedIds });
  await notifyWorkspaceChanged(workspaceId);
};

export const resCheckAvailability = (workspaceId: string) =>
  invoke<AvailabilityResult[]>("res_check_availability", { workspaceId });

export async function resGetIcon(resourceType: ResourceType, target: string): Promise<string | null> {
  if (resourceType === "link") return null;
  const key = `${resourceType}:${target.toLowerCase()}`;
  if (shellIconCache.has(key)) return shellIconCache.get(key) ?? null;
  try {
    const icon = await invoke<string | null>("res_get_icon", { resourceType, target });
    shellIconCache.set(key, icon ?? null);
    return icon ?? null;
  } catch {
    return null;
  }
}

export const resLaunchSingle = (id: string) =>
  invoke<LaunchItemResult>("res_launch_single", { id });

export const resLaunchBatch = (workspaceId: string, resourceIds?: string[]) =>
  invoke<BatchLaunchSummary>("res_launch_batch", {
    workspaceId,
    resourceIds: resourceIds ?? null,
  });

export const wsCancelLaunch = () => invoke<void>("ws_cancel_launch");
export const resPickFile = () => invoke<string | null>("res_pick_file");
export const resPickFolder = () => invoke<string | null>("res_pick_folder");

export const openWorkspaceWindow = () => invoke<void>("open_workspace_window");
export const openLibraryWindow = () => invoke<void>("open_library_window");
