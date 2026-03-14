#!/usr/bin/env node
/**
 * CRM Seed Script
 *
 * Populates the CRM with realistic sample data via the API Gateway.
 *
 * Usage:
 *   node scripts/seed-crm.js
 *   node scripts/seed-crm.js --gateway http://localhost:5000
 */

const GATEWAY = process.argv.includes('--gateway')
  ? process.argv[process.argv.indexOf('--gateway') + 1]
  : 'http://localhost:5000'

const ADMIN_EMAIL = process.env.ADMIN_EMAIL ?? 'admin@example.com'
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? 'Admin1234!'

// ─── Helpers ─────────────────────────────────────────────────────────────────

let token = null

async function request(method, path, body) {
  const headers = { 'Content-Type': 'application/json' }
  if (token) headers['Authorization'] = `Bearer ${token}`

  const res = await fetch(`${GATEWAY}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  })

  if (!res.ok) {
    const text = await res.text()
    throw new Error(`${method} ${path} → ${res.status}: ${text}`)
  }

  const text = await res.text()
  return text ? JSON.parse(text) : null
}

const post = (path, body) => request('POST', path, body)
const get  = (path)       => request('GET',  path)

function log(msg) { process.stdout.write(`  ${msg}\n`) }
function section(msg) { process.stdout.write(`\n▸ ${msg}\n`) }

// ─── Auth ─────────────────────────────────────────────────────────────────────

async function login() {
  section('Authenticating')
  const result = await post('/auth/api/login/login', {
    email: ADMIN_EMAIL,
    password: ADMIN_PASSWORD,
  })
  token = result.token
  log(`Logged in as ${ADMIN_EMAIL}`)
}

// ─── Accounts ─────────────────────────────────────────────────────────────────

async function seedAccounts() {
  section('Creating accounts')

  const accounts = await Promise.all([
    post('/accounts/api/accounts', {
      name: 'Acme Corporation',
      industry: 0,        // Technology
      size: 3,            // Enterprise
      website: 'https://acme.example.com',
      city: 'San Francisco',
      state: 'CA',
      country: 'US',
    }),
    post('/accounts/api/accounts', {
      name: 'Globex Industries',
      industry: 2,        // Healthcare
      size: 2,            // Large
      website: 'https://globex.example.com',
      city: 'Chicago',
      state: 'IL',
      country: 'US',
    }),
    post('/accounts/api/accounts', {
      name: 'Initech Solutions',
      industry: 0,        // Technology
      size: 1,            // Medium
      website: 'https://initech.example.com',
      city: 'Austin',
      state: 'TX',
      country: 'US',
    }),
    post('/accounts/api/accounts', {
      name: 'Umbrella Retail',
      industry: 3,        // Retail
      size: 2,            // Large
      city: 'New York',
      state: 'NY',
      country: 'US',
    }),
    post('/accounts/api/accounts', {
      name: 'Stark Manufacturing',
      industry: 4,        // Manufacturing
      size: 1,            // Medium
      city: 'Detroit',
      state: 'MI',
      country: 'US',
    }),
  ])

  for (const a of accounts) log(`  ${a.name} (${a.accountId})`)
  return accounts
}

// ─── Contacts ─────────────────────────────────────────────────────────────────

async function seedContacts(accounts, adminId) {
  section('Creating contacts')

  const [acme, globex, initech, umbrella, stark] = accounts

  const contacts = await Promise.all([
    // Acme
    post('/contacts/api/contacts', { firstName: 'Alice',   lastName: 'Chen',    email: 'alice.chen@acme.example.com',     phone: '415-555-0101', status: 2, accountId: acme.accountId,     ownerId: adminId }),
    post('/contacts/api/contacts', { firstName: 'Bob',     lastName: 'Martinez',email: 'bob.martinez@acme.example.com',   phone: '415-555-0102', status: 1, accountId: acme.accountId,     ownerId: adminId }),
    // Globex
    post('/contacts/api/contacts', { firstName: 'Carol',   lastName: 'Singh',   email: 'carol.singh@globex.example.com',  phone: '312-555-0201', status: 1, accountId: globex.accountId,   ownerId: adminId }),
    post('/contacts/api/contacts', { firstName: 'David',   lastName: 'Kim',     email: 'david.kim@globex.example.com',    phone: '312-555-0202', status: 0, accountId: globex.accountId,   ownerId: adminId }),
    // Initech
    post('/contacts/api/contacts', { firstName: 'Eve',     lastName: 'Johnson', email: 'eve.johnson@initech.example.com', phone: '512-555-0301', status: 1, accountId: initech.accountId,  ownerId: adminId }),
    post('/contacts/api/contacts', { firstName: 'Frank',   lastName: 'Liu',     email: 'frank.liu@initech.example.com',   phone: '512-555-0302', status: 0, accountId: initech.accountId,  ownerId: adminId }),
    // Umbrella
    post('/contacts/api/contacts', { firstName: 'Grace',   lastName: 'Patel',   email: 'grace.patel@umbrella.example.com',phone: '212-555-0401', status: 2, accountId: umbrella.accountId, ownerId: adminId }),
    // Stark
    post('/contacts/api/contacts', { firstName: 'Henry',   lastName: 'Torres',  email: 'henry.torres@stark.example.com',  phone: '313-555-0501', status: 0, accountId: stark.accountId,    ownerId: adminId }),
    post('/contacts/api/contacts', { firstName: 'Irene',   lastName: 'Nakamura',email: 'irene.nakamura@stark.example.com',phone: '313-555-0502', status: 3, accountId: stark.accountId,    ownerId: adminId }),
  ])

  for (const c of contacts) log(`  ${c.firstName} ${c.lastName} (${c.contactId})`)
  return contacts
}

// ─── Deals ────────────────────────────────────────────────────────────────────

async function seedDeals(accounts, contacts, adminId) {
  section('Creating deals')

  const [acme, globex, initech, umbrella, stark] = accounts
  const [alice, bob, carol, david, eve, frank, grace, henry] = contacts

  const future = (days) => new Date(Date.now() + days * 86400000).toISOString()

  const deals = await Promise.all([
    post('/deals/api/deals', { title: 'Acme Platform Upgrade',       accountId: acme.accountId,     stage: 2, value: 125000, probability: 70, expectedCloseDate: future(30),  ownerId: adminId }),
    post('/deals/api/deals', { title: 'Acme Support Contract',       accountId: acme.accountId,     stage: 1, value: 48000,  probability: 50, expectedCloseDate: future(45),  ownerId: adminId }),
    post('/deals/api/deals', { title: 'Globex Analytics Suite',      accountId: globex.accountId,   stage: 1, value: 89000,  probability: 40, expectedCloseDate: future(60),  ownerId: adminId }),
    post('/deals/api/deals', { title: 'Initech Cloud Migration',     accountId: initech.accountId,  stage: 0, value: 210000, probability: 20, expectedCloseDate: future(90),  ownerId: adminId }),
    post('/deals/api/deals', { title: 'Umbrella POS Integration',    accountId: umbrella.accountId, stage: 3, value: 67500,  probability: 90, expectedCloseDate: future(14),  ownerId: adminId }),
    post('/deals/api/deals', { title: 'Stark ERP Rollout',           accountId: stark.accountId,    stage: 0, value: 155000, probability: 15, expectedCloseDate: future(120), ownerId: adminId }),
  ])

  for (const d of deals) log(`  ${d.title} (${d.dealId})`)

  // Add contacts to deals
  section('Linking contacts to deals')
  await Promise.all([
    post(`/deals/api/deals/${deals[0].dealId}/contacts`, { contactId: alice.contactId, role: 0 }),  // DecisionMaker
    post(`/deals/api/deals/${deals[0].dealId}/contacts`, { contactId: bob.contactId,   role: 1 }),  // Influencer
    post(`/deals/api/deals/${deals[1].dealId}/contacts`, { contactId: alice.contactId, role: 2 }),  // Champion
    post(`/deals/api/deals/${deals[2].dealId}/contacts`, { contactId: carol.contactId, role: 0 }),  // DecisionMaker
    post(`/deals/api/deals/${deals[2].dealId}/contacts`, { contactId: david.contactId, role: 1 }),  // Influencer
    post(`/deals/api/deals/${deals[3].dealId}/contacts`, { contactId: eve.contactId,   role: 2 }),  // Champion
    post(`/deals/api/deals/${deals[3].dealId}/contacts`, { contactId: frank.contactId, role: 1 }),  // Influencer
    post(`/deals/api/deals/${deals[4].dealId}/contacts`, { contactId: grace.contactId, role: 0 }),  // DecisionMaker
    post(`/deals/api/deals/${deals[5].dealId}/contacts`, { contactId: henry.contactId, role: 1 }),  // Influencer
  ])
  log('Done')

  return deals
}

// ─── Activities ───────────────────────────────────────────────────────────────

async function seedActivities(accounts, contacts, deals, adminId) {
  section('Creating activities')

  const [acme, globex, initech, umbrella, stark] = accounts
  const [alice, bob, carol, david, eve, frank, grace, henry, irene] = contacts
  const past   = (days) => new Date(Date.now() - days * 86400000).toISOString()
  const future = (days) => new Date(Date.now() + days * 86400000).toISOString()

  const activities = await Promise.all([
    post('/activities/api/activities', { type: 0, subject: 'Discovery call with Alice Chen',       contactId: alice.contactId, dealId: deals[0].dealId, accountId: acme.accountId,     ownerId: adminId, scheduledAt: past(10),   notes: 'Discussed platform pain points. Strong interest in automation features.' }),
    post('/activities/api/activities', { type: 3, subject: 'Send contract for review',             contactId: alice.contactId, dealId: deals[0].dealId, accountId: acme.accountId,     ownerId: adminId, scheduledAt: future(3) }),
    post('/activities/api/activities', { type: 1, subject: 'Follow-up: Acme support pricing',      contactId: bob.contactId,   dealId: deals[1].dealId, accountId: acme.accountId,     ownerId: adminId, scheduledAt: past(5),    notes: 'Sent pricing deck. Bob forwarded to procurement.' }),
    post('/activities/api/activities', { type: 2, subject: 'Globex demo presentation',             contactId: carol.contactId, dealId: deals[2].dealId, accountId: globex.accountId,   ownerId: adminId, scheduledAt: future(7),  notes: 'Live demo of analytics dashboard scheduled.' }),
    post('/activities/api/activities', { type: 0, subject: 'Intro call with David Kim',            contactId: david.contactId,                           accountId: globex.accountId,   ownerId: adminId, scheduledAt: past(2),    notes: 'New stakeholder. Needs technical deep-dive.' }),
    post('/activities/api/activities', { type: 4, subject: 'Initech cloud migration scope notes',  contactId: eve.contactId,   dealId: deals[3].dealId, accountId: initech.accountId,  ownerId: adminId,                          notes: 'Estimated 18-month migration timeline. 3 legacy systems involved.' }),
    post('/activities/api/activities', { type: 2, subject: 'Umbrella POS pilot review',            contactId: grace.contactId, dealId: deals[4].dealId, accountId: umbrella.accountId, ownerId: adminId, scheduledAt: future(2) }),
    post('/activities/api/activities', { type: 0, subject: 'Stark ERP requirements call',          contactId: henry.contactId, dealId: deals[5].dealId, accountId: stark.accountId,    ownerId: adminId, scheduledAt: future(14), notes: 'Initial scoping call. Budget not yet approved.' }),
    post('/activities/api/activities', { type: 3, subject: 'Re-engage Irene Nakamura',             contactId: irene.contactId,                           accountId: stark.accountId,    ownerId: adminId, scheduledAt: future(5),  notes: 'Churned contact — check in on new budget cycle.' }),
    post('/activities/api/activities', { type: 1, subject: 'Quarterly newsletter',                                                                                                      ownerId: adminId, scheduledAt: past(1) }),
  ])

  log(`Created ${activities.length} activities`)
}

// ─── Main ─────────────────────────────────────────────────────────────────────

async function main() {
  console.log(`\nCRM Seed Script — ${GATEWAY}\n${'─'.repeat(50)}`)

  await login()

  // Get admin UserId from the JWT
  const payload = JSON.parse(Buffer.from(token.split('.')[1], 'base64').toString())
  const adminId = payload['UserId']
  log(`Admin UserId: ${adminId}`)

  const accounts   = await seedAccounts()
  const contacts   = await seedContacts(accounts, adminId)
  const deals      = await seedDeals(accounts, contacts, adminId)
  await seedActivities(accounts, contacts, deals, adminId)

  console.log(`\n${'─'.repeat(50)}\nSeed complete.\n`)
}

main().catch(err => {
  console.error('\nSeed failed:', err.message)
  process.exit(1)
})
