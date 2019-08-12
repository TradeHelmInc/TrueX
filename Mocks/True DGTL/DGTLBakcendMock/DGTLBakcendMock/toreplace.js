if (USE_MOCK_API) {
  const CLOB_API_IP = process.env.npm_config_mock_local ? "127.0.0.1" : "10.10.2.100";
  const CLOB_API_PORT = process.env.npm_config_mock_port || 9000;
  const CLOB_API_HISTORY_PORT = process.env.npm_config_mock_history_port || 29409;
  const MOCK_ENDPOINT = CLOB_API_IP + ':' + CLOB_API_PORT;
  const MOCK_HISTORY_ENDPOINT = CLOB_API_IP + ':' + CLOB_API_HISTORY_PORT;

    mockConfig = {
        core: {
            endpoints: {
                client: MOCK_ENDPOINT,
                history: MOCK_HISTORY_ENDPOINT
            }
        }
    }
}