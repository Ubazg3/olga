// Move-by-move replay viewer for /Admin/Games/{id}.
// The Razor page emits:
//   #board[data-initial="<32 dark-square chars> <side> <halfmove>"]
//   #move-list[data-moves='[{n, ba, ff, fr, tf, tr}, ...]']
// We render the initial position, then step forward by replacing the
// 32-character snapshot from each move's BoardAfter field.

(() => {
    const boardEl = document.getElementById("board");
    const listEl  = document.getElementById("move-list");
    if (!boardEl || !listEl) return;

    const initialState = boardEl.dataset.initial;
    const moves = JSON.parse(listEl.dataset.moves);

    // current move index: -1 = initial, 0..N-1 = after each move
    let cur = -1;
    let playing = false;
    let playTimer = null;

    // ----- board rendering -----

    // Build the 64 cell DOM nodes once.
    const cells = [];
    for (let r = 7; r >= 0; r--) {
        for (let f = 0; f < 8; f++) {
            const cell = document.createElement("div");
            cell.className = "cell " + (((f + r) & 1) === 0 ? "dark" : "light");
            cell.dataset.file = f;
            cell.dataset.rank = r;
            boardEl.appendChild(cell);
            cells.push(cell);
        }
    }

    function getCell(file, rank) {
        // Display order: rank 7 row first; within row file 0..7.
        const row = 7 - rank;
        const idx = row * 8 + file;
        return cells[idx];
    }

    function clearBoard() {
        for (const cell of cells) {
            cell.classList.remove("last");
            cell.innerHTML = "";
        }
    }

    function placePiece(file, rank, code) {
        const cell = getCell(file, rank);
        if (!cell) return;
        const piece = document.createElement("div");
        const color = (code === "w" || code === "W") ? "white" : "black";
        const isKing = (code === "W" || code === "B");
        piece.className = "piece " + color + (isKing ? " king" : "");
        cell.appendChild(piece);
    }

    function renderState(serialized, lastFrom, lastTo) {
        clearBoard();
        const flat = serialized.split(" ")[0];
        let idx = 0;
        for (let r = 0; r < 8; r++) {
            for (let f = 0; f < 8; f++) {
                if (((f + r) & 1) !== 0) continue;
                const code = flat.charAt(idx++);
                if (code !== ".") placePiece(f, r, code);
            }
        }
        if (lastFrom) getCell(lastFrom.f, lastFrom.r)?.classList.add("last");
        if (lastTo)   getCell(lastTo.f,   lastTo.r  )?.classList.add("last");
    }

    function highlightMove(idx) {
        for (const el of listEl.querySelectorAll(".move")) {
            el.classList.toggle("active",
                parseInt(el.dataset.idx, 10) === idx);
        }
    }

    function jumpTo(idx) {
        cur = Math.max(-1, Math.min(moves.length - 1, idx));
        if (cur === -1) {
            renderState(initialState, null, null);
        } else {
            const m = moves[cur];
            renderState(m.ba,
                { f: m.ff, r: m.fr },
                { f: m.tf, r: m.tr });
        }
        highlightMove(cur);
        updateButtons();
    }

    function updateButtons() {
        document.getElementById("btn-first").disabled = cur === -1;
        document.getElementById("btn-prev").disabled  = cur === -1;
        document.getElementById("btn-next").disabled  = cur === moves.length - 1;
        document.getElementById("btn-last").disabled  = cur === moves.length - 1;
        document.getElementById("btn-play").textContent = playing ? "❚❚ Pause" : "▶ Play";
    }

    // ----- controls -----

    document.getElementById("btn-first").addEventListener("click", () => {
        stopPlay(); jumpTo(-1);
    });
    document.getElementById("btn-prev").addEventListener("click", () => {
        stopPlay(); jumpTo(cur - 1);
    });
    document.getElementById("btn-next").addEventListener("click", () => {
        stopPlay(); jumpTo(cur + 1);
    });
    document.getElementById("btn-last").addEventListener("click", () => {
        stopPlay(); jumpTo(moves.length - 1);
    });
    document.getElementById("btn-play").addEventListener("click", () => {
        if (playing) { stopPlay(); return; }
        playing = true;
        updateButtons();
        playTimer = setInterval(() => {
            if (cur >= moves.length - 1) { stopPlay(); return; }
            jumpTo(cur + 1);
        }, 700);
    });

    function stopPlay() {
        playing = false;
        if (playTimer) { clearInterval(playTimer); playTimer = null; }
        updateButtons();
    }

    listEl.addEventListener("click", (e) => {
        const el = e.target.closest(".move");
        if (!el) return;
        stopPlay();
        jumpTo(parseInt(el.dataset.idx, 10));
    });

    // Keyboard nav
    document.addEventListener("keydown", (e) => {
        if (e.key === "ArrowRight") { stopPlay(); jumpTo(cur + 1); }
        if (e.key === "ArrowLeft")  { stopPlay(); jumpTo(cur - 1); }
        if (e.key === "Home")       { stopPlay(); jumpTo(-1); }
        if (e.key === "End")        { stopPlay(); jumpTo(moves.length - 1); }
        if (e.key === " ")          { document.getElementById("btn-play").click(); e.preventDefault(); }
    });

    jumpTo(-1);
})();
