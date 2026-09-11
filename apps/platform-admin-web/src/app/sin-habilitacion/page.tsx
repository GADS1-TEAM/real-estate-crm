import Link from "next/link";

export default function DisabledUserPage() {
  return <main className="access-page"><section className="access-card"><div className="lock-mark">×</div><span className="overline">Acceso</span><h1>Usuario no habilitado</h1><p>Tu identidad fue reconocida, pero todavía no tiene acceso a Platform Admin. Contactá al administrador de la plataforma.</p><Link className="button button-outline button-regular" href="/login">Volver al acceso</Link></section></main>;
}
