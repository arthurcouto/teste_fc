const LEDGER = "/api/ledger";
const CONSOLIDATION = "/api/consolidation";

const money = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });

function say(id, message, failed) {
  const target = document.getElementById(id);
  target.textContent = failed ? `Erro: ${message}` : message;
  target.classList.toggle("failed", Boolean(failed));
}

function submitting(form, busy) {
  form.querySelector('button[type="submit"]').disabled = busy;
}

async function call(url, options) {
  const response = await fetch(url, options);
  const body = response.status === 204 ? null : await response.json().catch(() => null);

  if (!response.ok) {
    throw new Error(body?.detail ?? `A requisição falhou com status ${response.status}.`);
  }

  return body;
}

function fill(table, rows, cells) {
  const body = table.querySelector("tbody");
  body.replaceChildren();

  if (rows.length === 0) {
    const empty = document.createElement("tr");
    const cell = document.createElement("td");
    cell.colSpan = table.querySelectorAll("thead th").length;
    cell.textContent = "Nenhum resultado para o período.";
    empty.append(cell);
    body.append(empty);
    return;
  }

  for (const row of rows) {
    const line = document.createElement("tr");

    for (const value of cells(row)) {
      const cell = document.createElement("td");
      cell.textContent = value;
      line.append(cell);
    }

    body.append(line);
  }
}

function showScreen(name) {
  for (const section of document.querySelectorAll("main section")) {
    section.hidden = section.id !== name;
  }
  for (const button of document.querySelectorAll("nav button")) {
    const selected = button.dataset.screen === name;
    button.classList.toggle("active", selected);
    button.setAttribute("aria-selected", String(selected));
  }
}

document.querySelectorAll("nav button").forEach((button) => {
  button.addEventListener("click", () => showScreen(button.dataset.screen));
});

document.getElementById("record").addEventListener("submit", async (event) => {
  event.preventDefault();
  const form = event.currentTarget;
  const data = new FormData(form);

  submitting(form, true);

  try {
    say("record-feedback", "Registrando...");
    const entry = await call(`${LEDGER}/entries/`, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({
        type: data.get("type"),
        amount: Number(data.get("amount")),
        competenceDate: data.get("competenceDate"),
        description: data.get("description") || null
      })
    });

    say("record-feedback", `Lançamento registrado em ${entry.competenceDate}.`);
    form.reset();
  } catch (failure) {
    say("record-feedback", failure.message, true);
  } finally {
    submitting(form, false);
  }
});

document.getElementById("list").addEventListener("submit", async (event) => {
  event.preventDefault();
  const form = event.currentTarget;
  const data = new FormData(form);
  const query = new URLSearchParams({ from: data.get("from"), to: data.get("to") });

  submitting(form, true);

  try {
    say("list-feedback", "Consultando...");
    const page = await call(`${LEDGER}/entries/?${query}`);

    fill(document.getElementById("entries-table"), page.items, (entry) => [
      entry.competenceDate,
      entry.type === "credit" ? "Crédito" : "Débito",
      money.format(entry.amount),
      entry.description ?? ""
    ]);

    say("list-feedback", `${page.totalCount} lançamento(s) no período.`);
  } catch (failure) {
    say("list-feedback", failure.message, true);
  } finally {
    submitting(form, false);
  }
});

document.getElementById("balance-query").addEventListener("submit", async (event) => {
  event.preventDefault();
  const form = event.currentTarget;
  const data = new FormData(form);
  const query = new URLSearchParams({ from: data.get("from"), to: data.get("to") });

  submitting(form, true);

  try {
    say("balance-feedback", "Consultando...");
    const series = await call(`${CONSOLIDATION}/daily-balances/?${query}`);

    fill(document.getElementById("balance-table"), series, (day) => [
      day.competenceDate,
      money.format(day.totalCredits),
      money.format(day.totalDebits),
      money.format(day.balance),
      day.entryCount
    ]);

    say("balance-feedback", `${series.length} dia(s) apurado(s).`);
  } catch (failure) {
    say("balance-feedback", failure.message, true);
  } finally {
    submitting(form, false);
  }
});

const today = new Date().toLocaleDateString("en-CA");
document.querySelectorAll('input[type="date"]').forEach((input) => {
  input.value = today;
});
