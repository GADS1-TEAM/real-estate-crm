import Link from "next/link";

export default function LoginPage() {
  return <main className="access-page"><section className="access-card"><div className="brand-lockup access-brand"><span className="brand-mark">b</span><div className="brand-copy"><strong>Platform Admin</strong><span>Pack Studio</span></div></div><span className="overline">Acceso seguro</span><h1>Ingresá a Platform Admin</h1><p>Administrá definiciones versionadas, validaciones y releases de la plataforma.</p><button className="button button-primary button-regular" type="button" disabled title="Requiere integración OIDC/Keycloak">Continuar con tu organización</button><small>El acceso se valida mediante la sesión corporativa. La integración OIDC/Keycloak todavía no está conectada en este entorno.</small><Link href="/">Volver a la pantalla principal</Link></section></main>;
}
