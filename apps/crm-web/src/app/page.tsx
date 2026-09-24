import { redirect } from "next/navigation";
import Link from "next/link";

export default function Page() {
  if (process.env.CLOUDFLARE_STATIC_EXPORT === "true") {
    return <main><Link href="/inicio">Abrir CRM</Link></main>;
  }
  redirect("/inicio");
}
