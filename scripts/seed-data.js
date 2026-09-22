const http = require('http');

const BFF_URL = 'http://localhost:5137/mutations';

function postMutation(name, payload) {
  return new Promise((resolve, reject) => {
    const data = JSON.stringify(payload);
    const req = http.request(`${BFF_URL}/${name}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(data)
      }
    }, (res) => {
      let body = '';
      res.on('data', chunk => body += chunk);
      res.on('end', () => {
        if (res.statusCode >= 200 && res.statusCode < 300) {
          try {
            resolve(JSON.parse(body));
          } catch {
            resolve(body);
          }
        } else {
          console.error(`[ERR] ${name} failed (${res.statusCode}):`, body);
          resolve(null);
        }
      });
    });
    req.on('error', err => {
      console.error(`[NET-ERR] ${name}:`, err.message);
      resolve(null);
    });
    req.write(data);
    req.end();
  });
}

async function runSeeding() {
  console.log('🚀 Iniciando carga de datos de prueba en la base de datos CRM...\n');

  // 1. EMPRESAS (Companies)
  console.log('--- Creating Companies ---');
  const companies = [
    { displayName: 'Desarrollos Inmobiliarios Puerto Madero S.A.', phone: '+54 11 4321-8800', email: 'contacto@puertomaderodev.com.ar', notes: 'Desarrolladora principal de Torres del Este y Madero Central.' },
    { displayName: 'Constructora del Plata S.R.L.', phone: '+54 11 4789-1122', email: 'info@constructoradelplata.com', notes: 'Empresa especializada en edificación residencial y dúplex.' },
    { displayName: 'Estudio Jurídico Inmobiliario & Asesores', phone: '+54 11 4371-5544', email: 'legales@estudioinmobiliario.com.ar', notes: 'Estudio notarial y legal para escrituras y fideicomisos.' },
    { displayName: 'Grupo Inversor Capital Real Estate', phone: '+54 11 5234-9900', email: 'inversiones@grupocapital.com', notes: 'Fondo privado de inversión en bienes raíces urbanos.' },
    { displayName: 'Fiduciaria Urbana S.A.', phone: '+54 11 4812-3344', email: 'operaciones@fiduciariaurbana.com', notes: 'Administración de fideicomisos al costo en CABA.' },
    { displayName: 'Arquitectura & Diseño Urbano S.A.', phone: '+54 11 4775-6677', email: 'proyectos@adurbano.com.ar', notes: 'Estudio de arquitectura y remodelaciones de alto standing.' },
    { displayName: 'Bienes Raíces Cordilleranos S.R.L.', phone: '+54 294 442-8899', email: 'patagonia@brcordilleranos.com', notes: 'Aliados estratégicos para desarrollos en Patagonia y Nordelta.' },
    { displayName: 'Inmobiliaria Norte & Asociados', phone: '+54 11 4791-0022', email: 'ventas@inmobiliarianorte.com.ar', notes: 'Red aliada de corredores inmobiliarios Zona Norte.' }
  ];

  const createdCompanies = [];
  for (const comp of companies) {
    const res = await postMutation('createCompany', comp);
    if (res && res.partyId) {
      createdCompanies.push(res);
      console.log(` ✅ Empresa creada: ${comp.displayName} (${res.partyId})`);
    }
  }

  // 2. PROPIEDADES (Properties)
  console.log('\n--- Creating Properties ---');
  const properties = [
    {
      propertyId: 'PROP-101',
      propertyTypeCode: 'APARTMENT',
      location: { province: 'Buenos Aires', locality: 'Capital Federal', neighborhood: 'Palermo Soho', street: 'Gorriti', streetNumber: '4800' },
      surfaceM2: 85.5,
      physicalAttributes: { rooms: 3, bedrooms: 2, bathrooms: 2, garages: 1 }
    },
    {
      propertyId: 'PROP-102',
      propertyTypeCode: 'HOUSE',
      location: { province: 'Buenos Aires', locality: 'San Isidro', neighborhood: 'Las Lomas', street: 'Av. del Libertador', streetNumber: '16200' },
      surfaceM2: 320.0,
      physicalAttributes: { rooms: 6, bedrooms: 4, bathrooms: 3, garages: 2 }
    },
    {
      propertyId: 'PROP-103',
      propertyTypeCode: 'COMMERCIAL',
      location: { province: 'Buenos Aires', locality: 'Capital Federal', neighborhood: 'Retiro', street: 'Av. Leandro N. Alem', streetNumber: '850' },
      surfaceM2: 140.0,
      physicalAttributes: { rooms: 4, bedrooms: 0, bathrooms: 2, garages: 1 }
    },
    {
      propertyId: 'PROP-104',
      propertyTypeCode: 'APARTMENT',
      location: { province: 'Buenos Aires', locality: 'Capital Federal', neighborhood: 'Puerto Madero', street: 'Juana Manso', streetNumber: '1100' },
      surfaceM2: 210.0,
      physicalAttributes: { rooms: 4, bedrooms: 3, bathrooms: 3, garages: 2 }
    },
    {
      propertyId: 'PROP-105',
      propertyTypeCode: 'COMMERCIAL',
      location: { province: 'Buenos Aires', locality: 'Capital Federal', neighborhood: 'Belgrano R', street: 'Zapiola', streetNumber: '2100' },
      surfaceM2: 95.0,
      physicalAttributes: { rooms: 2, bedrooms: 0, bathrooms: 1, garages: 0 }
    },
    {
      propertyId: 'PROP-106',
      propertyTypeCode: 'APARTMENT',
      location: { province: 'Buenos Aires', locality: 'Capital Federal', neighborhood: 'Recoleta', street: 'Av. Las Heras', streetNumber: '2300' },
      surfaceM2: 52.0,
      physicalAttributes: { rooms: 2, bedrooms: 1, bathrooms: 1, garages: 0 }
    },
    {
      propertyId: 'PROP-107',
      propertyTypeCode: 'LAND',
      location: { province: 'Buenos Aires', locality: 'Tigre', neighborhood: 'Nordelta', street: 'Av. de los Lagos', streetNumber: '500' },
      surfaceM2: 800.0,
      physicalAttributes: { rooms: 0, bedrooms: 0, bathrooms: 0, garages: 0 }
    },
    {
      propertyId: 'PROP-108',
      propertyTypeCode: 'HOUSE',
      location: { province: 'Buenos Aires', locality: 'Vicente López', neighborhood: 'Olivos', street: 'Maipú', streetNumber: '1400' },
      surfaceM2: 210.0,
      physicalAttributes: { rooms: 5, bedrooms: 3, bathrooms: 2, garages: 2 }
    },
    {
      propertyId: 'PROP-109',
      propertyTypeCode: 'APARTMENT',
      location: { province: 'Buenos Aires', locality: 'Capital Federal', neighborhood: 'Caballito', street: 'Av. Pedro Goyena', streetNumber: '900' },
      surfaceM2: 38.0,
      physicalAttributes: { rooms: 1, bedrooms: 1, bathrooms: 1, garages: 0 }
    },
    {
      propertyId: 'PROP-110',
      propertyTypeCode: 'HOUSE',
      location: { province: 'Buenos Aires', locality: 'Capital Federal', neighborhood: 'Villa Urquiza', street: 'Triunvirato', streetNumber: '4500' },
      surfaceM2: 130.0,
      physicalAttributes: { rooms: 4, bedrooms: 3, bathrooms: 2, garages: 1 }
    }
  ];

  for (const prop of properties) {
    const res = await postMutation('createProperty', prop);
    if (res && res.propertyId) {
      console.log(` ✅ Propiedad creada: ${prop.propertyId} - ${prop.location.neighborhood} (${prop.propertyTypeCode})`);
    }
  }

  // 3. PUBLICACIONES / CAPTACIONES (Listings)
  console.log('\n--- Creating Listings ---');
  const listings = [
    {
      listingId: 'LST-101',
      propertyId: 'PROP-101',
      operationTypeCode: 'SALE',
      canonicalTitle: 'Depto 3 Ambientes con Balcón y Cochera en Palermo Soho',
      canonicalDescription: 'Excelente piso alto super luminoso con vista abierta a la ciudad.',
      commercialTerms: { price: { amount: 185000, currency: 'USD' }, expenses: { amount: 45000, currency: 'ARS' } },
      availability: { availableFrom: '2026-09-22T00:00:00Z', status: 'IMMEDIATE' },
      responsibleUserId: '6bc4221b-460c-41d7-8080-922261e0fd01'
    },
    {
      listingId: 'LST-102',
      propertyId: 'PROP-102',
      operationTypeCode: 'SALE',
      canonicalTitle: 'Gran Casa Quinta con Piscina y Jardín en Las Lomas de San Isidro',
      canonicalDescription: 'Propiedad única sobre lote de 600m2 con arboleda frondosa.',
      commercialTerms: { price: { amount: 540000, currency: 'USD' }, expenses: { amount: 0, currency: 'USD' } },
      availability: { availableFrom: '2026-10-01T00:00:00Z', status: 'AGREED' },
      responsibleUserId: '6bc4221b-460c-41d7-8080-922261e0fd01'
    },
    {
      listingId: 'LST-103',
      propertyId: 'PROP-103',
      operationTypeCode: 'RENT',
      canonicalTitle: 'Oficina Comercial de Categoría en Catalinas Alem',
      canonicalDescription: 'Planta dividida en despachos con recepción y sala de reuniones.',
      commercialTerms: { price: { amount: 2200, currency: 'USD' }, expenses: { amount: 120000, currency: 'ARS' } },
      availability: { availableFrom: '2026-09-22T00:00:00Z', status: 'IMMEDIATE' },
      responsibleUserId: '6bc4221b-460c-41d7-8080-922261e0fd01'
    },
    {
      listingId: 'LST-104',
      propertyId: 'PROP-104',
      operationTypeCode: 'SALE',
      canonicalTitle: 'Exclusivo Penthouse en Puerto Madero con Terraza y Parrilla',
      canonicalDescription: 'Vistas panoramic de 360 grados al río y reserva ecológica.',
      commercialTerms: { price: { amount: 890000, currency: 'USD' }, expenses: { amount: 180000, currency: 'ARS' } },
      availability: { availableFrom: '2026-09-22T00:00:00Z', status: 'IMMEDIATE' },
      responsibleUserId: '6bc4221b-460c-41d7-8080-922261e0fd01'
    },
    {
      listingId: 'LST-105',
      propertyId: 'PROP-105',
      operationTypeCode: 'RENT',
      canonicalTitle: 'Local Comercial a la Calle sobre Zapiola en Belgrano R',
      canonicalDescription: 'Ideal rubro gastronómico, indumentaria o estudio profesional.',
      commercialTerms: { price: { amount: 1500, currency: 'USD' }, expenses: { amount: 30000, currency: 'ARS' } },
      availability: { availableFrom: '2026-10-15T00:00:00Z', status: 'AGREED' },
      responsibleUserId: '6bc4221b-460c-41d7-8080-922261e0fd01'
    },
    {
      listingId: 'LST-106',
      propertyId: 'PROP-106',
      operationTypeCode: 'SALE',
      canonicalTitle: 'Depto 2 Ambientes Reciclado a Nuevo en Recoleta',
      canonicalDescription: 'Detalles de categoría, pisos de roble de eslavonia y cocina completa.',
      commercialTerms: { price: { amount: 135000, currency: 'USD' }, expenses: { amount: 38000, currency: 'ARS' } },
      availability: { availableFrom: '2026-09-22T00:00:00Z', status: 'IMMEDIATE' },
      responsibleUserId: '6bc4221b-460c-41d7-8080-922261e0fd01'
    },
    {
      listingId: 'LST-107',
      propertyId: 'PROP-107',
      operationTypeCode: 'SALE',
      canonicalTitle: 'Lote al Lago Central en Barrio Exclusivo de Nordelta',
      canonicalDescription: 'Excelente orientación con amarra propia y proyecto aprobado.',
      commercialTerms: { price: { amount: 220000, currency: 'USD' }, expenses: { amount: 85000, currency: 'ARS' } },
      availability: { availableFrom: '2026-09-22T00:00:00Z', status: 'IMMEDIATE' },
      responsibleUserId: '6bc4221b-460c-41d7-8080-922261e0fd01'
    },
    {
      listingId: 'LST-108',
      propertyId: 'PROP-108',
      operationTypeCode: 'SALE',
      canonicalTitle: 'Casa Residencial en Olivos a Metros de la Estación',
      canonicalDescription: 'Desarrollada en dos plantas con patio verde, quincho y cochera doble.',
      commercialTerms: { price: { amount: 390000, currency: 'USD' }, expenses: { amount: 0, currency: 'USD' } },
      availability: { availableFrom: '2026-09-22T00:00:00Z', status: 'IMMEDIATE' },
      responsibleUserId: '6bc4221b-460c-41d7-8080-922261e0fd01'
    }
  ];

  for (const lst of listings) {
    const res = await postMutation('createListing', lst);
    if (res && res.listingId) {
      console.log(` ✅ Publicación creada: ${lst.listingId} - ${lst.canonicalTitle}`);
      // Activar publicación
      await postMutation('activateListing', { listingId: lst.listingId });
    }
  }

  // 4. REQUERIMIENTOS / BÚSQUEDAS (Requirements)
  console.log('\n--- Creating Requirements ---');
  const requirements = [
    {
      requirementId: '22222222-1111-4000-8000-000000000001',
      seekerPartyIds: ['d0000000-0000-0000-0000-000000000001'],
      operationTypeCode: 'SALE',
      propertyTypeCode: 'APARTMENT',
      locationCriteria: { province: 'Buenos Aires', locality: 'Capital Federal', neighborhood: 'Palermo Soho' },
      financialCriteria: { minAmount: 140000, maxAmount: 200000, currency: 'USD' },
      responsibleUserId: '6bc4221b-460c-41d7-8080-922261e0fd01',
      originCode: 'WEB'
    },
    {
      requirementId: '22222222-1111-4000-8000-000000000002',
      seekerPartyIds: ['d0000000-0000-0000-0000-000000000002'],
      operationTypeCode: 'SALE',
      propertyTypeCode: 'HOUSE',
      locationCriteria: { province: 'Buenos Aires', locality: 'San Isidro', neighborhood: 'Las Lomas' },
      financialCriteria: { minAmount: 400000, maxAmount: 600000, currency: 'USD' },
      responsibleUserId: '6bc4221b-460c-41d7-8080-922261e0fd01',
      originCode: 'PORTAL'
    },
    {
      requirementId: '22222222-1111-4000-8000-000000000003',
      seekerPartyIds: ['d0000000-0000-0000-0000-000000000003'],
      operationTypeCode: 'RENT',
      propertyTypeCode: 'COMMERCIAL',
      locationCriteria: { province: 'Buenos Aires', locality: 'Capital Federal', neighborhood: 'Retiro' },
      financialCriteria: { minAmount: 1500, maxAmount: 3000, currency: 'USD' },
      responsibleUserId: '6bc4221b-460c-41d7-8080-922261e0fd01',
      originCode: 'RECOMMENDATION'
    },
    {
      requirementId: '22222222-1111-4000-8000-000000000004',
      seekerPartyIds: ['d0000000-0000-0000-0000-000000000004'],
      operationTypeCode: 'SALE',
      propertyTypeCode: 'APARTMENT',
      locationCriteria: { province: 'Buenos Aires', locality: 'Capital Federal', neighborhood: 'Puerto Madero' },
      financialCriteria: { minAmount: 700000, maxAmount: 1000000, currency: 'USD' },
      responsibleUserId: '6bc4221b-460c-41d7-8080-922261e0fd01',
      originCode: 'DIRECT'
    },
    {
      requirementId: '22222222-1111-4000-8000-000000000005',
      seekerPartyIds: ['d0000000-0000-0000-0000-000000000005'],
      operationTypeCode: 'SALE',
      propertyTypeCode: 'LAND',
      locationCriteria: { province: 'Buenos Aires', locality: 'Tigre', neighborhood: 'Nordelta' },
      financialCriteria: { minAmount: 180000, maxAmount: 250000, currency: 'USD' },
      responsibleUserId: '6bc4221b-460c-41d7-8080-922261e0fd01',
      originCode: 'WEB'
    }
  ];

  for (const req of requirements) {
    const res = await postMutation('createRequirement', req);
    if (res && res.requirementId) {
      console.log(` ✅ Requerimiento creado: ${req.requirementId} (${req.propertyTypeCode} - ${req.operationTypeCode})`);
    }
  }

  // 5. ACTIVIDADES / HISTORIAL DE ACCIONES (Activities)
  console.log('\n--- Recording Activities ---');
  const activities = [
    {
      activityTypeCode: 'CALL',
      occurredAt: '2026-09-20T10:30:00Z',
      recordedByUserId: '6bc4221b-460c-41d7-8080-922261e0fd01',
      primaryPartyId: 'd0000000-0000-0000-0000-000000000001',
      propertyId: 'PROP-101',
      listingId: 'LST-101',
      description: 'Llamada saliente a cliente para ofrecer departamento 3 ambientes en Palermo Soho.',
      result: 'Cliente interesado. Solicitó ficha técnica por WhatsApp y coordinar visita.'
    },
    {
      activityTypeCode: 'VISIT',
      occurredAt: '2026-09-21T15:00:00Z',
      recordedByUserId: '6bc4221b-460c-41d7-8080-922261e0fd01',
      primaryPartyId: 'd0000000-0000-0000-0000-000000000001',
      propertyId: 'PROP-101',
      listingId: 'LST-101',
      description: 'Visita presencial realizada al inmueble Gorriti 4800 con el comprador interesado.',
      result: 'Le gustó mucho la luminosidad y la distribución. Quedó en consultar con su banco el crédito hipotecario.'
    },
    {
      activityTypeCode: 'MEETING',
      occurredAt: '2026-09-18T11:00:00Z',
      recordedByUserId: '6bc4221b-460c-41d7-8080-922261e0fd01',
      primaryPartyId: 'd0000000-0000-0000-0000-000000000002',
      propertyId: 'PROP-102',
      listingId: 'LST-102',
      description: 'Reunión presencial en oficina con propietarios de casa quinta en San Isidro para autorizar la venta exclusiva.',
      result: 'Firma de autorización comercial exclusiva por 90 días por valor de USD 540.000.'
    },
    {
      activityTypeCode: 'EMAIL',
      occurredAt: '2026-09-19T09:15:00Z',
      recordedByUserId: '6bc4221b-460c-41d7-8080-922261e0fd01',
      primaryPartyId: 'd0000000-0000-0000-0000-000000000003',
      propertyId: 'PROP-103',
      listingId: 'LST-103',
      description: 'Envío de propuesta de alquiler comercial y borrador de contrato para oficina Catalinas Alem.',
      result: 'En revisión por el departamento legal del cliente.'
    },
    {
      activityTypeCode: 'INSPECTION',
      occurredAt: '2026-09-17T14:30:00Z',
      recordedByUserId: '6bc4221b-460c-41d7-8080-922261e0fd01',
      primaryPartyId: 'd0000000-0000-0000-0000-000000000004',
      propertyId: 'PROP-104',
      listingId: 'LST-104',
      description: 'Inspección fotográfica profesional y medición de superficies m2 para Penthouse Puerto Madero.',
      result: 'Material fotográfico cargado en HD para portales y catálogo.'
    },
    {
      activityTypeCode: 'NOTE',
      occurredAt: '2026-09-21T18:00:00Z',
      recordedByUserId: '6bc4221b-460c-41d7-8080-922261e0fd01',
      primaryPartyId: 'd0000000-0000-0000-0000-000000000005',
      propertyId: 'PROP-107',
      listingId: 'LST-107',
      description: 'Nota interna: comprador de Nordelta solicita plano del lote y reglamento del barrio cerrado.',
      result: 'Documentación enviada por email.'
    }
  ];

  for (const act of activities) {
    const res = await postMutation('recordActivity', act);
    if (res && res.activityId) {
      console.log(` ✅ Actividad registrada: ${act.activityTypeCode} - ${act.description.substring(0, 45)}...`);
    }
  }

  console.log('\n🎉 Cargados con éxito todos los datos de prueba del CRM (Empresas, Propiedades, Publicaciones, Requerimientos, Actividades).\n');
}

runSeeding();
