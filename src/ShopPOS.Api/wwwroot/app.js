const shopIdInput = document.getElementById('shopId');
const apiKeyInput = document.getElementById('apiKey');
const statsEl = document.getElementById('stats');
const salesList = document.getElementById('salesList');
const productsList = document.getElementById('productsList');
const refreshBtn = document.getElementById('refreshBtn');

function headers() {
  return { 'X-Api-Key': apiKeyInput.value.trim() };
}

function formatMoney(value) {
  return `Rs. ${Number(value || 0).toLocaleString('en-PK', { minimumFractionDigits: 0 })}`;
}

function formatDate(value) {
  return new Date(value).toLocaleString('en-PK');
}

async function fetchJson(url) {
  const res = await fetch(url, { headers: headers() });
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
  return res.json();
}

function renderStats(data) {
  statsEl.innerHTML = `
    <div class="stat"><div class="label">Today's Sales</div><div class="value">${data.todaySalesCount}</div></div>
    <div class="stat"><div class="label">Today's Revenue</div><div class="value">${formatMoney(data.todayRevenue)}</div></div>
    <div class="stat"><div class="label">Products</div><div class="value">${data.totalProducts}</div></div>
    <div class="stat"><div class="label">Low Stock</div><div class="value">${data.lowStock}</div></div>
    <div class="stat"><div class="label">Customers</div><div class="value">${data.totalCustomers}</div></div>
  `;
}

function renderSales(items) {
  if (!items.length) {
    salesList.innerHTML = '<p class="subtitle">No sales synced yet.</p>';
    return;
  }

  salesList.innerHTML = items.map(s => `
    <div class="row">
      <div>
        <strong>${s.receiptNo}</strong>
        <small>${formatDate(s.saleDate)} · ${s.paymentMethod}</small>
        ${s.customerPhone ? `<small>📱 ${s.customerPhone}</small>` : ''}
        ${s.customerEmail ? `<small>✉ ${s.customerEmail}</small>` : ''}
      </div>
      <span class="badge">${formatMoney(s.netTotal)}</span>
    </div>
  `).join('');
}

function renderProducts(items) {
  if (!items.length) {
    productsList.innerHTML = '<p class="subtitle">No products synced yet.</p>';
    return;
  }

  productsList.innerHTML = items.slice(0, 100).map(p => `
    <div class="row">
      <div>
        <strong>${p.name}</strong>
        <small>${p.category}${p.barcode ? ` · ${p.barcode}` : ''}</small>
      </div>
      <span class="badge ${p.stock <= 5 ? 'low' : ''}">${p.stock} · ${formatMoney(p.price)}</span>
    </div>
  `).join('');
}

async function loadDashboard() {
  const shopId = encodeURIComponent(shopIdInput.value.trim());
  document.querySelectorAll('.error').forEach(el => el.remove());

  try {
    const [summary, sales, products] = await Promise.all([
      fetchJson(`/api/dashboard/${shopId}`),
      fetchJson(`/api/sales/${shopId}?take=30`),
      fetchJson(`/api/products/${shopId}`)
    ]);

    renderStats(summary);
    renderSales(sales);
    renderProducts(products);
  } catch (err) {
    const div = document.createElement('div');
    div.className = 'error';
    div.textContent = `Could not load dashboard: ${err.message}. Check API key, shop ID, and that cloud API is running.`;
    document.body.insertBefore(div, statsEl);
  }
}

refreshBtn.addEventListener('click', loadDashboard);
shopIdInput.addEventListener('change', loadDashboard);
apiKeyInput.addEventListener('change', loadDashboard);

loadDashboard();
