export type RoleId = "vendedor" | "responsable" | "direccion" | "administradora";
export type Permission =
  | "party.read"
  | "party.write"
  | "property.read"
  | "property.write"
  | "commercial.write"
  | "analytics.read"
  | "admin.read"
  | "admin.write";

export interface RoleDefinition {
  id: RoleId;
  name: string;
  description: string;
  scope: string;
  permissions: Permission[];
}

export interface CrmPersona {
  name: string;
  roleId: RoleId;
  roleLabel: string;
  scope: string;
  startScreen: string;
}

export const crmPersonas: CrmPersona[] = [
  { name: "Martín Quiroga", roleId: "vendedor", roleLabel: "Vendedor", scope: "Sus oportunidades y contactos asignados", startScreen: "INI-01" },
  { name: "Lucía Ferrari", roleId: "vendedor", roleLabel: "Vendedora · captaciones", scope: "Captaciones y propiedades asignadas", startScreen: "PRP-01" },
  { name: "Rodrigo Vergara", roleId: "responsable", roleLabel: "Responsable comercial", scope: "Equipo comercial completo", startScreen: "INI-03" },
  { name: "Elena Vergara", roleId: "direccion", roleLabel: "Dirección", scope: "Indicadores y drill-down de la instalación", startScreen: "ANA-05" },
  { name: "Sofía Rendón", roleId: "administradora", roleLabel: "Administradora", scope: "Instalación completa", startScreen: "ADM-01" },
];

const permissionLabels: Record<Permission, string> = {
  "party.read": "Consultar contactos",
  "party.write": "Editar contactos",
  "property.read": "Consultar inmuebles",
  "property.write": "Editar inmuebles",
  "commercial.write": "Gestionar oportunidades",
  "analytics.read": "Consultar métricas",
  "admin.read": "Consultar administración",
  "admin.write": "Configurar la instalación",
};

export const roleDefinitions: RoleDefinition[] = [
  {
    id: "vendedor",
    name: "Vendedor",
    description: "Gestiona sus contactos, demanda y operaciones asignadas.",
    scope: "Sus oportunidades y contactos asignados",
    permissions: ["party.read", "party.write", "property.read", "commercial.write"],
  },
  {
    id: "responsable",
    name: "Responsable comercial",
    description: "Supervisa el equipo y puede intervenir en su embudo.",
    scope: "Equipo comercial completo",
    permissions: ["party.read", "party.write", "property.read", "property.write", "commercial.write", "analytics.read"],
  },
  {
    id: "direccion",
    name: "Dirección",
    description: "Consulta el rendimiento global y baja hasta la operación concreta.",
    scope: "Indicadores y drill-down de la instalación",
    permissions: ["party.read", "property.read", "analytics.read"],
  },
  {
    id: "administradora",
    name: "Administradora",
    description: "Configura usuarios, roles y catálogos de la instalación.",
    scope: "Instalación completa",
    permissions: ["party.read", "property.read", "analytics.read", "admin.read", "admin.write"],
  },
];

export function getRole(roleId: RoleId): RoleDefinition {
  return roleDefinitions.find((role) => role.id === roleId) ?? roleDefinitions[0];
}

export function hasPermission(roleId: RoleId, permission: Permission): boolean {
  return getRole(roleId).permissions.includes(permission);
}

export function permissionLabel(permission: Permission): string {
  return permissionLabels[permission];
}

export function permissionReason(roleId: RoleId, permission: Permission): string {
  if (hasPermission(roleId, permission)) return "Permitido por tu rol efectivo.";
  return `Tu rol ${getRole(roleId).name} no incluye «${permissionLabel(permission)}». Pedí habilitación a la administradora.`;
}
