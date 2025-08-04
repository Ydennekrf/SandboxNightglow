/* ───────── NPCs ───────── */
CREATE TABLE IF NOT EXISTS NPC (
    npc_id        TEXT PRIMARY KEY,
    name          TEXT NOT NULL,
    portrait_path TEXT NOT NULL,
    scene_path    TEXT,
    start_node_id TEXT,
    FOREIGN KEY (start_node_id) REFERENCES DialogNode(node_id) ON DELETE SET NULL
);

/* ───────── Dialog Nodes ───────── */
CREATE TABLE IF NOT EXISTS DialogNode (
    node_id       TEXT PRIMARY KEY,
    npc_id        TEXT NOT NULL,
    speaker       TEXT,
    portrait_path TEXT,
    text          TEXT NOT NULL,
    FOREIGN KEY (npc_id) REFERENCES NPC(npc_id) ON DELETE CASCADE
);

/* ───────── Choices ───────── */
CREATE TABLE IF NOT EXISTS Choice (
    choice_id    TEXT PRIMARY KEY,
    node_id      TEXT NOT NULL,
    sort_index   INTEGER DEFAULT 0,
    text         TEXT NOT NULL,
    next_node_id TEXT,
    FOREIGN KEY (node_id)      REFERENCES DialogNode(node_id) ON DELETE CASCADE,
    FOREIGN KEY (next_node_id) REFERENCES DialogNode(node_id) ON DELETE SET NULL
);

/* ───────── Actions & bridge ───────── */
CREATE TABLE IF NOT EXISTS Action (
    action_id INTEGER PRIMARY KEY AUTOINCREMENT,
    type      INTEGER NOT NULL,
    json_data TEXT                -- {"questId":"...", "delta":-2}
);

CREATE TABLE IF NOT EXISTS ChoiceAction (
    choice_id TEXT    NOT NULL,
    action_id INTEGER NOT NULL,
    PRIMARY KEY (choice_id, action_id),
    FOREIGN KEY (choice_id) REFERENCES Choice(choice_id) ON DELETE CASCADE,
    FOREIGN KEY (action_id) REFERENCES Action(action_id) ON DELETE CASCADE
);

/* helpful index */
CREATE INDEX IF NOT EXISTS idx_dialognode_npc ON DialogNode(npc_id);
