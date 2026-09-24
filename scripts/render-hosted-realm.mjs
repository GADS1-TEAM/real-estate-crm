import { readFile, mkdir, writeFile } from 'node:fs/promises';
import { resolve } from 'node:path';

const host = process.env.CRM_PUBLIC_HOST;
const password = process.env.CRM_INITIAL_ADMIN_PASSWORD;
if (!host || !/^[a-z0-9.-]+$/i.test(host) || !password || password === 'replace-me' || password.length < 16) {
  throw new Error('Se necesitan CRM_PUBLIC_HOST válido y CRM_INITIAL_ADMIN_PASSWORD de al menos 16 caracteres.');
}

const source = resolve('infra/keycloak/realm-export/crm-dev-realm.json');
const destination = resolve('infra/hosting/generated/realm.json');
const realm = JSON.parse(await readFile(source, 'utf8'));
const client = realm.clients.find((candidate) => candidate.clientId === 'operations-bff');
const admin = realm.users.find((candidate) => candidate.username === 'dev.administrador');
if (!client || !admin) throw new Error('El realm fuente no contiene el cliente y usuario de bootstrap esperados.');

client.redirectUris = [`https://${host}/signin-oidc`];
client.webOrigins = [`https://${host}`];
client.directAccessGrantsEnabled = false;
admin.credentials = [{ type: 'password', value: password, temporary: false }];
realm.users = [admin];
realm.sslRequired = 'external';

await mkdir(resolve('infra/hosting/generated'), { recursive: true });
await writeFile(destination, JSON.stringify(realm, null, 2), { mode: 0o600 });
console.log('Realm de bootstrap generado en infra/hosting/generated/realm.json.');
