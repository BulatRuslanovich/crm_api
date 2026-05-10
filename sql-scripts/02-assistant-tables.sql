CREATE TABLE assistant_conversation (
    conversation_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    usr_id          INT NOT NULL REFERENCES usr(usr_id) ON DELETE CASCADE,
    title           VARCHAR(255),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE assistant_message (
    message_id      BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    conversation_id BIGINT NOT NULL REFERENCES assistant_conversation(conversation_id) ON DELETE CASCADE,
    role            VARCHAR(20) NOT NULL,
    content         TEXT NOT NULL,
    tool_calls      TEXT,
    tool_call_id    VARCHAR(128),
    provider        VARCHAR(64),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_assistant_message_role CHECK (role IN ('user', 'assistant', 'tool', 'system'))
);

CREATE INDEX ix_assistant_conversation_usr_updated
    ON assistant_conversation(usr_id, updated_at DESC);

CREATE INDEX ix_assistant_message_conversation_created
    ON assistant_message(conversation_id, created_at);
