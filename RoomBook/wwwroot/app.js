let token = sessionStorage.getItem("token");
let rooms = [];

const $ = (id) => document.getElementById(id);
const say = (text) => ($("message").textContent = text || "");

async function api(method, url, body) {
  const res = await fetch(url, {
    method,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: "Bearer " + token } : {}),
    },
    body: body ? JSON.stringify(body) : undefined,
  });
  const data = res.status === 204 ? null : await res.json().catch(() => null);
  if (!res.ok) throw new Error((data && data.error) || `Request failed (${res.status})`);
  return data;
}

function show() {
  $("auth").classList.toggle("hidden", !!token);
  $("app").classList.toggle("hidden", !token);
  if (token) refresh();
}

async function refresh() {
  try {
    rooms = await api("GET", "/rooms");
    $("rooms").tBodies[0].innerHTML = rooms
      .map((r) => `<tr><td>${r.name}</td><td>${r.capacity}</td><td>${r.hourlyRate}</td></tr>`)
      .join("");
    $("room").innerHTML = rooms.map((r) => `<option value="${r.id}">${r.name}</option>`).join("");
    await loadBookings();
  } catch (e) {
    token = null;
    sessionStorage.removeItem("token");
    show();
    say(e.message);
  }
}

async function loadBookings() {
  const date = $("filter-date").value;
  const list = await api("GET", "/bookings" + (date ? `?date=${date}` : ""));
  const roomName = (id) => (rooms.find((r) => r.id === id) || {}).name;
  $("bookings").tBodies[0].innerHTML = list
    .map(
      (b) => `<tr><td>${roomName(b.roomId)}</td><td>${b.start.replace("T", " ")}</td><td>${b.end.replace("T", " ")}</td>
        <td>${b.attendees}</td><td>${b.price}</td><td><button data-cancel="${b.id}">Cancel</button></td></tr>`
    )
    .join("");
}

async function login(email, password) {
  const res = await api("POST", "/login", { email, password });
  token = res.token;
  sessionStorage.setItem("token", token);
  say("");
  show();
}

$("login-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  try { await login($("email").value, $("password").value); } catch (err) { say(err.message); }
});

$("register").addEventListener("click", async () => {
  try {
    await api("POST", "/register", { email: $("email").value, name: $("name").value, password: $("password").value });
    await login($("email").value, $("password").value);
  } catch (err) { say(err.message); }
});

$("logout").addEventListener("click", async () => {
  try { await api("POST", "/logout"); } catch { /* ignore */ }
  token = null;
  sessionStorage.removeItem("token");
  show();
});

$("book-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const withSeconds = (v) => (v.length === 16 ? v + ":00" : v);
  try {
    await api("POST", "/bookings", {
      roomId: Number($("room").value),
      start: withSeconds($("start").value),
      end: withSeconds($("end").value),
      attendees: Number($("attendees").value),
    });
    say("");
    await loadBookings();
  } catch (err) { say(err.message); }
});

$("filter-form").addEventListener("submit", (e) => {
  e.preventDefault();
  loadBookings().catch((err) => say(err.message));
});

$("bookings").addEventListener("click", async (e) => {
  const id = e.target.dataset.cancel;
  if (!id) return;
  try {
    await api("DELETE", `/bookings/${id}`);
    say("");
    await loadBookings();
  } catch (err) { say(err.message); }
});

show();
