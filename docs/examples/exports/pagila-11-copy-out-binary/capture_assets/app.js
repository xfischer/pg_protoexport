(function () {
  'use strict';

  function ready(fn) {
    if (document.readyState !== 'loading') fn();
    else document.addEventListener('DOMContentLoaded', fn);
  }

  ready(function () {
    var dataEl = document.getElementById('protoexport-data');
    if (!dataEl) return;
    var data;
    try { data = JSON.parse(dataEl.textContent); }
    catch (e) { console.error('Failed to parse protoexport-data', e); return; }

    renderMermaid(data.mermaidDiagrams || []);
    renderCards(data);
    initFieldHighlight();
    initScrollSpy(data);
    initGlossary(data.glossary || {});
    initFilterChips(data);
  });

  var FILTER_STATE_KEY = 'protoexport:filters';

  function initFilterChips(data) {
    var container = document.getElementById('filter-chips');
    if (!container) return;

    var counts = {};
    (data.cards || []).forEach(function (c) {
      counts[c.name] = (counts[c.name] || 0) + 1;
    });
    var names = Object.keys(counts).sort();

    var state = loadFilterState();

    var chips = [];
    chips.push(chipHtml('bytes-only', 'Bytes-only view', null, state.bytesOnly));
    chips.push(chipHtml('hide-frontend', 'Hide frontend', null, state.hideFrontend));
    chips.push(chipHtml('hide-backend', 'Hide backend', null, state.hideBackend));
    chips.push('<span class="chip-sep" aria-hidden="true"></span>');
    names.forEach(function (name) {
      var active = state.hiddenNames.indexOf(name) >= 0;
      chips.push(chipHtml('name:' + name, 'Hide ' + name, counts[name], active));
    });
    chips.push('<button type="button" class="chip chip-reset" data-action="reset">Reset</button>');
    container.innerHTML = chips.join('');

    applyFilterState(state);

    container.addEventListener('click', function (e) {
      var chip = e.target.closest('.chip');
      if (!chip) return;
      var action = chip.getAttribute('data-action');
      if (action === 'reset') {
        state = emptyFilterState();
      } else if (action === 'bytes-only') {
        state.bytesOnly = !state.bytesOnly;
      } else if (action === 'hide-frontend') {
        state.hideFrontend = !state.hideFrontend;
      } else if (action === 'hide-backend') {
        state.hideBackend = !state.hideBackend;
      } else if (action && action.indexOf('name:') === 0) {
        var n = action.slice('name:'.length);
        var i = state.hiddenNames.indexOf(n);
        if (i >= 0) state.hiddenNames.splice(i, 1);
        else state.hiddenNames.push(n);
      }
      saveFilterState(state);
      syncChipsActive(state);
      applyFilterState(state);
    });
  }

  function chipHtml(action, label, count, active) {
    return '<button type="button" class="chip' + (active ? ' is-active' : '') + '"'
      + ' data-action="' + esc(action) + '"'
      + ' aria-pressed="' + (active ? 'true' : 'false') + '">'
      + esc(label)
      + (count != null ? ' <span class="chip-count">' + count + '</span>' : '')
      + '</button>';
  }

  function syncChipsActive(state) {
    var container = document.getElementById('filter-chips');
    if (!container) return;
    Array.prototype.forEach.call(container.querySelectorAll('.chip'), function (chip) {
      var action = chip.getAttribute('data-action');
      var active = false;
      if (action === 'bytes-only') active = state.bytesOnly;
      else if (action === 'hide-frontend') active = state.hideFrontend;
      else if (action === 'hide-backend') active = state.hideBackend;
      else if (action && action.indexOf('name:') === 0) active = state.hiddenNames.indexOf(action.slice('name:'.length)) >= 0;
      chip.classList.toggle('is-active', active);
      chip.setAttribute('aria-pressed', active ? 'true' : 'false');
    });
  }

  function applyFilterState(state) {
    document.body.classList.toggle('bytes-only', !!state.bytesOnly);
    var cards = document.querySelectorAll('.card');
    Array.prototype.forEach.call(cards, function (card) {
      var direction = card.getAttribute('data-direction');
      var name = card.getAttribute('data-message-name');
      var isFrontend = direction === 'C->S';
      var hide = false;
      if (state.hideFrontend && isFrontend) hide = true;
      if (state.hideBackend && !isFrontend) hide = true;
      if (state.hiddenNames.indexOf(name) >= 0) hide = true;
      card.classList.toggle('hidden', hide);
    });
  }

  function emptyFilterState() {
    return { bytesOnly: false, hideFrontend: false, hideBackend: false, hiddenNames: [] };
  }

  function loadFilterState() {
    try {
      var raw = sessionStorage.getItem(FILTER_STATE_KEY);
      if (raw) {
        var parsed = JSON.parse(raw);
        return {
          bytesOnly: !!parsed.bytesOnly,
          hideFrontend: !!parsed.hideFrontend,
          hideBackend: !!parsed.hideBackend,
          hiddenNames: Array.isArray(parsed.hiddenNames) ? parsed.hiddenNames.slice() : []
        };
      }
    } catch (e) { /* sessionStorage may be unavailable in file:// — ignore */ }
    return emptyFilterState();
  }

  function saveFilterState(state) {
    try { sessionStorage.setItem(FILTER_STATE_KEY, JSON.stringify(state)); } catch (e) { /* ignore */ }
  }

  function renderMermaid(diagrams) {
    var container = document.getElementById('overview-diagrams');
    if (!container || !diagrams.length) return;
    var nodes = diagrams.map(function (src, i) {
      var pre = document.createElement('pre');
      pre.className = 'mermaid';
      pre.id = 'overview-diagram-' + i;
      pre.textContent = src.trim();
      container.appendChild(pre);
      return pre;
    });
    var tries = 0;
    function attempt() {
      if (window.mermaid && typeof window.mermaid.run === 'function') {
        window.mermaid.initialize({ startOnLoad: false, securityLevel: 'strict', theme: 'neutral' });
        window.mermaid.run({ nodes: nodes }).catch(function (err) {
          console.warn('Mermaid render failed', err);
        });
      } else if (tries++ < 50) {
        setTimeout(attempt, 100);
      } else {
        console.warn('Mermaid library not loaded; diagrams will not render.');
      }
    }
    attempt();
  }

  function renderCards(data) {
    var container = document.getElementById('cards');
    if (!container) return;
    var interludes = (data.interludes || []).slice().sort(function (a, b) {
      return a.insertBeforeCardIdx - b.insertBeforeCardIdx;
    });
    var iPtr = 0;
    var parts = [];
    data.cards.forEach(function (card) {
      while (iPtr < interludes.length && interludes[iPtr].insertBeforeCardIdx <= card.idx) {
        parts.push(renderInterlude(interludes[iPtr]));
        iPtr++;
      }
      parts.push(renderCard(card));
    });
    while (iPtr < interludes.length) {
      parts.push(renderInterlude(interludes[iPtr]));
      iPtr++;
    }
    container.innerHTML = parts.join('');
  }

  function renderInterlude(interlude) {
    return '<section class="interlude" data-pattern="' + esc(interlude.patternId) + '">'
      + '<h3>' + esc(interlude.title) + '</h3>'
      + '<p>' + esc(interlude.body) + '</p>'
      + '</section>';
  }

  function renderCard(card) {
    var fieldsHtml = (card.fields || []).map(function (f) {
      return '<li data-field-id="' + esc(f.id) + '">'
        + '<span class="fname">' + esc(f.name) + '</span>'
        + (f.display ? '<span class="fval">' + esc(f.display) + '</span>' : '')
        + '<span class="frange">@' + f.offset + ' &middot; ' + f.length + 'B</span>'
        + '</li>';
    }).join('');

    var bytefieldHtml = renderBytefield(card);

    var jumpHtml = '';
    if (typeof card.correlatedCardIdx === 'number') {
      jumpHtml = '<p class="cancel-jump"><a href="#card-' + card.correlatedCardIdx + '" data-jump-to="' + card.correlatedCardIdx + '">'
        + '&rarr; Jump to query session (card #' + card.correlatedCardIdx + ')</a></p>';
    }

    return '<article class="card" id="card-' + card.idx + '" data-direction="' + esc(card.direction) + '" data-card-idx="' + card.idx + '" data-message-name="' + esc(card.name) + '">'
      + '<div class="card-head">'
      + '<span class="idx">#' + card.idx + '</span>'
      + '<span class="direction">' + esc(card.direction) + '</span>'
      + '<span class="name">' + esc(card.name) + '</span>'
      + '</div>'
      + '<p class="headline">' + esc(card.headline) + '</p>'
      + (card.rationale ? '<p class="rationale">' + esc(card.rationale) + '</p>' : '')
      + jumpHtml
      + (fieldsHtml ? '<ul class="fields">' + fieldsHtml + '</ul>' : '')
      + bytefieldHtml
      + '</article>';
  }

  var BYTEFIELD_ROW_BYTES = 16;

  function renderBytefield(card) {
    var total = card.lengthBytes | 0;
    if (total <= 0) return '';

    var fields = (card.fields || []).slice().sort(function (a, b) { return a.offset - b.offset; });

    var segments = [];
    var cursor = 0;
    fields.forEach(function (f) {
      if (f.length <= 0) return;
      if (cursor < f.offset) segments.push({ kind: 'gap', offset: cursor, length: f.offset - cursor });
      segments.push({ kind: 'field', id: f.id, name: f.name, display: f.display, offset: f.offset, length: f.length });
      cursor = f.offset + f.length;
    });
    if (cursor < total) segments.push({ kind: 'gap', offset: cursor, length: total - cursor });

    var rows = [];
    var currentRow = [];
    var usedInRow = 0;
    segments.forEach(function (seg) {
      var remaining = seg.length;
      var isFirstFragment = true;
      while (remaining > 0) {
        if (usedInRow === BYTEFIELD_ROW_BYTES) { rows.push(currentRow); currentRow = []; usedInRow = 0; }
        var take = Math.min(remaining, BYTEFIELD_ROW_BYTES - usedInRow);
        currentRow.push({
          kind: seg.kind,
          id: seg.id,
          name: seg.name,
          display: seg.display,
          offset: seg.offset + (seg.length - remaining),
          length: take,
          isFirstFragment: isFirstFragment,
          totalLength: seg.length
        });
        isFirstFragment = false;
        remaining -= take;
        usedInRow += take;
      }
    });
    if (currentRow.length) rows.push(currentRow);

    var html = '<div class="bytefield" style="--row-bytes:' + BYTEFIELD_ROW_BYTES + '">';
    rows.forEach(function (row, ri) {
      html += '<div class="bytefield-row">';
      var rowFilled = 0;
      row.forEach(function (c) {
        rowFilled += c.length;
        html += renderBytefieldCell(c);
      });
      if (rowFilled < BYTEFIELD_ROW_BYTES && ri === rows.length - 1) {
        html += '<div class="bytefield-cell pad" style="grid-column:span ' + (BYTEFIELD_ROW_BYTES - rowFilled) + '"></div>';
      }
      html += '</div>';
    });
    html += '</div>';
    return html;
  }

  function renderBytefieldCell(c) {
    if (c.kind === 'gap') {
      return '<div class="bytefield-cell no-field" style="grid-column:span ' + c.length + '"></div>';
    }
    var inner;
    if (c.isFirstFragment) {
      var nameHtml = '<span class="bf-name">' + esc(c.name) + '</span>';
      var valHtml = c.display ? '<span class="bf-val">' + esc(c.display) + '</span>' : '';
      var offHtml = '<span class="bf-off">@' + c.offset + ' &middot; ' + c.totalLength + 'B</span>';
      inner = nameHtml + valHtml + offHtml;
    } else {
      inner = '<span class="bf-cont">&#8629; cont.</span>';
    }
    return '<div class="bytefield-cell" style="grid-column:span ' + c.length + '" data-field-id="' + esc(c.id) + '" title="' + esc(c.name) + '">' + inner + '</div>';
  }

  function initFieldHighlight() {
    document.addEventListener('mouseover', function (e) {
      var target = e.target.closest('[data-field-id]');
      if (!target) return;
      toggleHighlight(target.getAttribute('data-field-id'), true);
    });
    document.addEventListener('mouseout', function (e) {
      var target = e.target.closest('[data-field-id]');
      if (!target) return;
      toggleHighlight(target.getAttribute('data-field-id'), false);
    });
  }

  function toggleHighlight(id, on) {
    if (!id) return;
    var nodes = document.querySelectorAll('[data-field-id="' + cssEscape(id) + '"]');
    for (var i = 0; i < nodes.length; i++) {
      nodes[i].classList.toggle('is-active', on);
    }
  }

  function initScrollSpy(data) {
    var cards = document.querySelectorAll('.card');
    if (!cards.length) return;
    var observer = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          entry.target.classList.add('is-current');
          var idx = parseInt(entry.target.getAttribute('data-card-idx'), 10);
          var card = data.cards[idx];
          if (card) updateSidebar(card.stateAfter || {});
        } else {
          entry.target.classList.remove('is-current');
        }
      });
    }, { rootMargin: '-40% 0px -55% 0px' });
    cards.forEach(function (c) { observer.observe(c); });
  }

  function updateSidebar(state) {
    setText('state-conn', state.connState || '—');
    setText('state-tx', state.txStatus || '—');
    setText('state-pid', state.backendPid != null ? state.backendPid : '—');
    var copyText = state.copyMode
      ? state.copyMode + (state.copyFormat ? ' (' + state.copyFormat.toLowerCase() + ')' : '')
      : '(none)';
    setText('state-copy', copyText);
    setText('state-prepared', summarize(state.prepared));
    setText('state-portals', summarize(state.portals));
    setText('state-params', summarize(state.serverParams));
  }

  function summarize(map) {
    if (!map) return '(none)';
    var keys = Object.keys(map);
    if (!keys.length) return '(none)';
    return keys.map(function (k) { return k + ' = ' + map[k]; }).join('\n');
  }

  function setText(id, value) {
    var el = document.getElementById(id);
    if (el) el.textContent = value;
  }

  function initGlossary(glossary) {
    var terms = Object.keys(glossary);
    if (!terms.length) return;
    var cardEls = document.querySelectorAll('.card .headline, .card .name');
    cardEls.forEach(function (el) {
      terms.forEach(function (term) {
        var entry = glossary[term];
        var re = new RegExp('\\b' + escapeRegExp(term) + '\\b');
        if (re.test(el.textContent) && !el.querySelector('.gloss')) {
          el.innerHTML = el.innerHTML.replace(re, function (match) {
            var doc = entry.docLink
              ? ' <a href="' + esc(entry.docLink) + '" target="_blank" rel="noopener">docs</a>'
              : '';
            var src = entry.srcLink
              ? ' <a href="' + esc(entry.srcLink) + '" target="_blank" rel="noopener">src</a>'
              : '';
            return '<span class="gloss">' + match
              + '<span class="tooltip">' + esc(entry.definition) + doc + src + '</span>'
              + '</span>';
          });
        }
      });
    });
  }

  function esc(s) {
    return String(s).replace(/[&<>"']/g, function (c) {
      return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c];
    });
  }
  function escapeRegExp(s) { return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); }
  function cssEscape(s) { return s.replace(/"/g, '\\"'); }
})();
