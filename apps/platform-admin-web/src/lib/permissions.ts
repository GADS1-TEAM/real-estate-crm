import type { PlatformPermission } from "@/lib/types";

export type PlatformRoleId = "configurator" | "release-manager" | "compliance" | "support" | "platform-admin";

export interface PlatformRole {
  id: PlatformRoleId;
  name: string;
  shortName: string;
  permissions: PlatformPermission[];
}

export const platformRoles: PlatformRole[] = [
  { id: "configurator", name: "Configuradora de producto", shortName: "Configuradora", permissions: ["VIEW", "EDIT_DRAFT"] },
  { id: "release-manager", name: "Release manager", shortName: "Release manager", permissions: ["VIEW", "EDIT_DRAFT", "PUBLISH", "DEPRECATE", "MANAGE_PILOTS", "PROMOTE"] },
  { id: "compliance", name: "Compliance / Legal", shortName: "Compliance / Legal", permissions: ["VIEW", "EDIT_DRAFT", "PUBLISH"] },
  { id: "support", name: "Soporte de plataforma", shortName: "Soporte", permissions: ["VIEW", "ADMIN_CORRECTION", "VIEW_AUDIT"] },
  { id: "platform-admin", name: "Admin de plataforma", shortName: "Admin plataforma", permissions: ["VIEW", "EDIT_DRAFT", "PUBLISH", "DEPRECATE", "MANAGE_PILOTS", "PROMOTE", "ADMIN_CORRECTION", "VIEW_AUDIT"] },
];

const permissionNames: Record<PlatformPermission, string> = {
  VIEW: "VIEW",
  EDIT_DRAFT: "EDIT_DRAFT",
  PUBLISH: "PUBLISH",
  DEPRECATE: "DEPRECATE",
  MANAGE_PILOTS: "MANAGE_PILOTS",
  PROMOTE: "PROMOTE",
  ADMIN_CORRECTION: "ADMIN_CORRECTION",
  VIEW_AUDIT: "VIEW_AUDIT",
};

export function getRole(roleId: string): PlatformRole {
  return platformRoles.find((role) => role.id === roleId) ?? platformRoles[0];
}

export function can(roleId: string, permission: PlatformPermission, scope?: "compliance"): boolean {
  const role = getRole(roleId);
  if (scope === "compliance" && permission === "PUBLISH") return role.permissions.includes(permission) && ["compliance", "platform-admin"].includes(role.id);
  return role.permissions.includes(permission);
}

export function explainPermission(roleId: string, permission: PlatformPermission): string {
  if (can(roleId, permission)) return `Tenés el permiso ${permissionNames[permission]}.`;
  const owners = platformRoles.filter((role) => role.permissions.includes(permission)).map((role) => role.name);
  return `Podés ver la configuración, pero requiere ${permissionNames[permission]}. Lo tienen ${owners.join(" y ")}.`;
}

export function roleLabel(roleId: string): string {
  return getRole(roleId).shortName;
}
