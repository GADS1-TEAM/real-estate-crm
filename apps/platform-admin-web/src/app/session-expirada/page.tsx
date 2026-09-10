import Link from "next/link";

export default function ExpiredSessionPage() {
  return <main className="access-page"><section className="access-card"><div className="lock-mark">!</div><span className="overline">Sesión</span><h1>Tu sesión expiró</h1><p>Volvé a autenticarte para continuar trabajando con la configuración de la plataforma.</p><Link className="button button-primary button-regular" href="/login">Iniciar sesión</Link></section></main>;
}
