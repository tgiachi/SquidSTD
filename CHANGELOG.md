## [0.8.0](https://github.com/tgiachi/squid-std/compare/v0.7.0...v0.8.0) (2026-06-28)

## [0.7.0](https://github.com/tgiachi/squid-std/compare/v0.6.0...v0.7.0) (2026-06-26)

### Features

* add generated registration attributes ([e6cfee7](https://github.com/tgiachi/squid-std/commit/e6cfee794099641f5586d652cea892b1ac0b6d02))
* **core:** add FNV-1a ChecksumUtils ([7cdb74f](https://github.com/tgiachi/squid-std/commit/7cdb74fcfead4afab93400698afbc2e980adfd37))
* **events:** add IEventListener<T> and EventBusOptions contracts ([30b0d57](https://github.com/tgiachi/squid-std/commit/30b0d5768d0d5c30320c36eb5284efaf70091ee4))
* **events:** DI-native listener auto-subscription ([f1940ef](https://github.com/tgiachi/squid-std/commit/f1940ef3e5bebbc5ca695d246a8912c4fa552ef2))
* **events:** parallel dispatch, catch-all, fault isolation, delegate subscribe ([fcb84b1](https://github.com/tgiachi/squid-std/commit/fcb84b11c9a29734e9fcd3148829de3e8069bc88))
* generate config section registrations ([092e52e](https://github.com/tgiachi/squid-std/commit/092e52e4ed31b949f335fe41e1b1ef998ea5f8a1))
* generate event listener registrations ([f8786d8](https://github.com/tgiachi/squid-std/commit/f8786d8ef1f709ef2bd8f4698c60fa7c20d7c942))
* generate job handler registrations ([e16ab20](https://github.com/tgiachi/squid-std/commit/e16ab201e5be5b13ca2997ca009a71f277bb0b79))
* generate lua script module registrations ([d879f42](https://github.com/tgiachi/squid-std/commit/d879f42f1100671e44eaf814ceffa279d040a731))
* generate standard service registrations ([febf319](https://github.com/tgiachi/squid-std/commit/febf319573fbf85e639de8414d49f330cfadde1d))
* **network:** add atomic SwapCodec for mid-connection codec upgrade ([eefd529](https://github.com/tgiachi/squid-std/commit/eefd529339edc008cfcb466aed9505a26db65d50))
* **network:** add ConnectionPipeline per-connection descriptor ([3e76b9f](https://github.com/tgiachi/squid-std/commit/3e76b9fa598e0ca1da39115b2815cda8c63035bd))
* **network:** add ITransportCodec contract and test codec ([e7e2fd7](https://github.com/tgiachi/squid-std/commit/e7e2fd70a4070706b0dd83724639120172d90086))
* **network:** apply per-connection transport codec on send and receive ([23ed22b](https://github.com/tgiachi/squid-std/commit/23ed22bcac54435f3af195041c8af51c9c068ccc))
* **persistence:** atomic per-type binary snapshot service ([f5a6e45](https://github.com/tgiachi/squid-std/commit/f5a6e4531a4b7ff590402760ddec5cd2c671d26c))
* **persistence:** binary append-only journal service ([6aa4b74](https://github.com/tgiachi/squid-std/commit/6aa4b74bde8cca5aa4db745df96da1c9b4f9e3c9))
* **persistence:** entity descriptor registry ([b3d822f](https://github.com/tgiachi/squid-std/commit/b3d822f96d6db0f841b4aaca806a8e0a24a93bcd))
* **persistence:** fixed-binary journal and snapshot codecs ([1711672](https://github.com/tgiachi/squid-std/commit/1711672b55dee3036b453a872851b1ad0fbcd539))
* **persistence:** in-memory entity store with write-ordered journaling ([c9a242e](https://github.com/tgiachi/squid-std/commit/c9a242ebed2c5d894bf6cdc324b6a5ee4d1092fa))
* **persistence:** lifecycle service with snapshot+journal replay and autosave ([532ad55](https://github.com/tgiachi/squid-std/commit/532ad559e41b00728d06536dc386ab08c740c345))
* **persistence:** MessagePack serializer provider and DI entity registration ([bf2a046](https://github.com/tgiachi/squid-std/commit/bf2a046ed40cc94172df228e9d25932eaf073b72))
* **persistence:** scaffold persistence packages and abstractions ([4f3fce2](https://github.com/tgiachi/squid-std/commit/4f3fce22b43ab59407d025812f6fd3aaca98cdff))
* **persistence:** state store and serializer-injected entity descriptor ([27b7cfe](https://github.com/tgiachi/squid-std/commit/27b7cfeb600c96e2228799cbd83dad818ee46eb6))
* require event listener registration attribute ([2b99967](https://github.com/tgiachi/squid-std/commit/2b99967414b6eb54211f48d66369833d0a03aa99))

### Bug Fixes

* **network:** keep receive history in sync with decoded output and clarify per-connection pipeline factory ([8b9d447](https://github.com/tgiachi/squid-std/commit/8b9d44785c4373bb3c475f223173708e8572da6b))

## [0.6.0](https://github.com/tgiachi/squid-std/compare/v0.5.1...v0.6.0) (2026-06-25)

### Features

* add AddSqsMessaging registration and sqs:// connection string parsing ([f193b33](https://github.com/tgiachi/squid-std/commit/f193b3392e3d02c986b0d9c3bf89cccea34f8f4b))
* add ASP.NET Core AddSquidStdTelemetry registration ([d4707b4](https://github.com/tgiachi/squid-std/commit/d4707b46b76e9bf9489d7f5104be3292fa056ec0))
* add SqsQueueProvider with redrive-to-DLQ over LocalStack-tested SQS ([05ab288](https://github.com/tgiachi/squid-std/commit/05ab288d5133733ada4e189bcdedb6f01393ee4e))
* add SqsTopicProvider with SNS+SQS fan-out ([5278178](https://github.com/tgiachi/squid-std/commit/52781784b4847846870c3f19e529cb6885e1a52f))
* add SquidStd.Aws.Abstractions with shared AwsConfigEntry ([23fde7d](https://github.com/tgiachi/squid-std/commit/23fde7de4c4d2d410db9f934af134b91d5f2f49f))
* add SquidStd.Telemetry.Abstractions with TelemetryOptions and ActivitySource convention ([9d18cee](https://github.com/tgiachi/squid-std/commit/9d18ceef5b0acf85a760389bdfbe87efee4339a2))
* add SquidStd.Telemetry.OpenTelemetry pipeline helper with tracing export ([886b08a](https://github.com/tgiachi/squid-std/commit/886b08afd492830c0b0122e7f2a04073b09faf37))
* add TelemetryService and worker AddSquidStdTelemetry registration ([5aa4b64](https://github.com/tgiachi/squid-std/commit/5aa4b64cf820b9b8578463c482eb4eb11dc833be))
* bridge the SquidStd metrics snapshot to OpenTelemetry instruments ([a480b21](https://github.com/tgiachi/squid-std/commit/a480b211074e7776f3ac8797772d1187545891cc))
* compose AwsConfigEntry in S3StorageOptions ([651820e](https://github.com/tgiachi/squid-std/commit/651820ea7c1722d3cfba775ccae719a627866997))
* scaffold SquidStd.Messaging.Sqs with SqsOptions, name sanitizer and AWS client factory ([a6dc839](https://github.com/tgiachi/squid-std/commit/a6dc83906cbe0b6cfa5f903da2d5aea4500661ae))

## [0.5.1](https://github.com/tgiachi/squid-std/compare/v0.5.0...v0.5.1) (2026-06-25)

### Bug Fixes

* bump StackExchange.Redis to 3.0.7 ([1977dac](https://github.com/tgiachi/squid-std/commit/1977dac2bc8fe1b4ef46f4e2f2f88d39ace3b881))

## [0.5.0](https://github.com/tgiachi/squid-std/compare/v0.4.0...v0.5.0) (2026-06-24)

### Features

* **mail:** add AddMail registration with protocol-specific reader and timer-wheel pump ([839c85a](https://github.com/tgiachi/squid-std/commit/839c85a05b9149cc6babbf53635b9cc1ed19312f))
* **mail:** add AddMailSender registration for the SMTP sender ([f2458c4](https://github.com/tgiachi/squid-std/commit/f2458c44d9f6047f725eff2cb0ab2c281d0db5b8))
* **mail:** add IMailQueue and MailQueue publishing to the messaging queue ([1e626e0](https://github.com/tgiachi/squid-std/commit/1e626e088669e5dfe56c276d093c1695be33a485))
* **mail:** add ImapMailReader fetching unseen messages ([2e48a79](https://github.com/tgiachi/squid-std/commit/2e48a797687317f8144f8280b2cadf934ae2dd72))
* **mail:** add MailKitMailSender sending via SMTP with send events ([0d3d7e2](https://github.com/tgiachi/squid-std/commit/0d3d7e21a10ab292c87c3c8a093fa4d578808a93))
* **mail:** add MailMessage, MailReceivedEvent, IMailReader and MailOptions contracts ([ebb7ffa](https://github.com/tgiachi/squid-std/commit/ebb7ffa2f5202578ed7dea3c09bfd6156a74f447))
* **mail:** add MailPollingService publishing MailReceivedEvent on the timer wheel ([935c7be](https://github.com/tgiachi/squid-std/commit/935c7be0b4811c8847a989a33d0f09f19e626f3f))
* **mail:** add MailSendConsumerService and AddMailQueue registration ([226d0f1](https://github.com/tgiachi/squid-std/commit/226d0f11bcc33d38f7a28ce7d5272a0757245d37))
* **mail:** add MimeMessageMapper from MimeMessage to MailMessage ([92d18c0](https://github.com/tgiachi/squid-std/commit/92d18c01dea233e5b75bbba3f94699da44fb510f))
* **mail:** add outbound contracts (IMailSender, OutgoingMailMessage, SmtpOptions, send events) ([65cd6f4](https://github.com/tgiachi/squid-std/commit/65cd6f4eff7b9d0a21c253c8ab6ded56430d18fd))
* **mail:** add OutgoingMessageMapper to MimeMessage ([5943e83](https://github.com/tgiachi/squid-std/commit/5943e831d4d015f141de9bf8806b072d0ca6a351))
* **mail:** add Pop3MailReader with UIDL dedup and optional delete ([bf9e315](https://github.com/tgiachi/squid-std/commit/bf9e3155b4a7de71df8e9205d29e0ee1a7bf250b))
* **search:** add Elasticsearch IQueryable provider with async terminals ([a13e14f](https://github.com/tgiachi/squid-std/commit/a13e14fc5116955f84031217364833f4043f0cfa))
* **search:** add Elasticsearch options, transport helper, registration and index/delete/bulk service ([98d5a5a](https://github.com/tgiachi/squid-std/commit/98d5a5ae3c2816a0fee86167f076b9c4666cee5b))
* **search:** add IIndexableEntity, SearchIndexAttribute, ISearchService and SearchException ([df6daa2](https://github.com/tgiachi/squid-std/commit/df6daa2be5e337ff6729a14784598cae5e67f4d3))
* **search:** add LINQ-to-Elasticsearch expression translator ([f9f16a3](https://github.com/tgiachi/squid-std/commit/f9f16a3fb61bad6901f315ea2039026f48e43f32))
* **search:** add SearchIndexNameResolver with env-variable expansion ([d9f70e0](https://github.com/tgiachi/squid-std/commit/d9f70e0fd5a11777742946101d395be14a0c3901))
* **templates:** add squidstd-aspnetcore minimal API template ([58ec1a4](https://github.com/tgiachi/squid-std/commit/58ec1a4fdca7d52ae0b872f0a49c47bb3735bf10))
* **templates:** add squidstd-host console template ([5aacc48](https://github.com/tgiachi/squid-std/commit/5aacc483c05bab2649050386f7a756e4db3002c3))
* **templates:** add squidstd-manager ASP.NET template with messaging choice ([54f6958](https://github.com/tgiachi/squid-std/commit/54f69582a313536f780283fcaa4e48bc9b0d1759))
* **templates:** add squidstd-worker microservice template with messaging choice ([9642944](https://github.com/tgiachi/squid-std/commit/96429446940d796b83e00a0cbe46dca4d127f71a))

### Bug Fixes

* **mail:** pass explicit CancellationToken.None on failure-event publish ([ec121ad](https://github.com/tgiachi/squid-std/commit/ec121adeb8e76f38daf1248948ea6215ad848a63))
* **templates:** emit template content in pack (disable symbols, TFM content hook, posix paths) ([8881f32](https://github.com/tgiachi/squid-std/commit/8881f321444e066833749cf49e3a59cda7d25c04))

## [0.4.0](https://github.com/tgiachi/squid-std/compare/v0.3.0...v0.4.0) (2026-06-23)

### Features

* **core:** register a shared DirectoriesConfig in RegisterCoreServices ([d1f784c](https://github.com/tgiachi/squid-std/commit/d1f784cb98eb64c756dadbe1e6ad2734364fe252))
* **messaging:** add in-memory topic provider, facade/bridge registration and tests ([06cee70](https://github.com/tgiachi/squid-std/commit/06cee7025a0f04a3a2417ff5ac96061839c96522))
* **messaging:** add RabbitMQ fanout topic provider and registration ([83407ec](https://github.com/tgiachi/squid-std/commit/83407ec1ad08ab9c870738fcc41abe4a5b9e283c))
* **messaging:** add topic pub/sub contracts, facade and event-bus bridge ([5ed2778](https://github.com/tgiachi/squid-std/commit/5ed2778fa8a5652395ab7f3bf5f5ba4fcdfcc248))
* **templating:** add AddTemplating DI registration ([96dfc73](https://github.com/tgiachi/squid-std/commit/96dfc739ef5bfd4f33c6a5981f89522a84223d36))
* **templating:** add SquidStd.Templating with Scriban renderer and named registry ([1a73232](https://github.com/tgiachi/squid-std/commit/1a7323267b000b3df7bf3554ac904a3e9cc11b28))
* **workers-manager:** add AddWorkerManager registration extension ([38c990e](https://github.com/tgiachi/squid-std/commit/38c990e8c519f854d427de2fdca25335c074f930))
* **workers-manager:** add config, status-change event and enqueue request types ([12874cd](https://github.com/tgiachi/squid-std/commit/12874cde4069dd4c8465a9dc6a05b75a7138c3a1))
* **workers-manager:** add HeartbeatCollectorService folding heartbeats into the registry ([fcd6bee](https://github.com/tgiachi/squid-std/commit/fcd6bee5c84b17ec7ca430ab2e7589ba515f735c))
* **workers-manager:** add HTTP endpoints for querying workers and enqueuing jobs ([b92c6b7](https://github.com/tgiachi/squid-std/commit/b92c6b75b82dd9d83ed1fe5e68b68884dcba590d))
* **workers-manager:** add JobScheduler publishing jobs to the queue ([4bc4ce9](https://github.com/tgiachi/squid-std/commit/4bc4ce90516c6e9d3a927a2fa95ad57c510e4efe))
* **workers-manager:** add WorkerOfflineSweepService marking stale workers offline ([23efd37](https://github.com/tgiachi/squid-std/commit/23efd3718995ef72c84d51db39777e31d7011b03))
* **workers-manager:** add WorkerRegistry folding heartbeats into worker state ([7d73cb9](https://github.com/tgiachi/squid-std/commit/7d73cb944c3620ea99634d538d29d4531831e35f))
* **workers:** add AddWorkers and AddJobHandler registration extensions ([fbb3033](https://github.com/tgiachi/squid-std/commit/fbb3033e482529b6fb9e19832c7bfdfa3ef4e939))
* **workers:** add IJobHandler and JobHandlerNotFoundException ([544b5e4](https://github.com/tgiachi/squid-std/commit/544b5e4b7e579f59c19b9737b659101e37361a7f))
* **workers:** add JobDispatcher routing jobs to handlers by name ([2e9d105](https://github.com/tgiachi/squid-std/commit/2e9d1053415e6724c5d7c5a7f2ed9c1decbb90aa))
* **workers:** add JobRequest, WorkerHeartbeat and WorkerInfo contracts ([2a74d58](https://github.com/tgiachi/squid-std/commit/2a74d589bd930ebb01e19420a665cde3ec03afd6))
* **workers:** add WorkerChannels conventional channel names ([94f44c0](https://github.com/tgiachi/squid-std/commit/94f44c0352499d65655d8b363aa3f19ea85392bd))
* **workers:** add WorkerConsumerService consuming and dispatching jobs ([420f685](https://github.com/tgiachi/squid-std/commit/420f685422e129b5c3aaad36388fdbfea9ab8699))
* **workers:** add WorkerHeartbeatService publishing periodic heartbeats ([d47ec85](https://github.com/tgiachi/squid-std/commit/d47ec8597208e866f3b90e7a573761ab704ca764))
* **workers:** add WorkersConfig config section ([1b82a79](https://github.com/tgiachi/squid-std/commit/1b82a796f37ce30d31bc2e748d8e68d6e3805993))
* **workers:** add WorkerState shared runtime state ([a3eff82](https://github.com/tgiachi/squid-std/commit/a3eff821fd88dfa1079863ac7108fc15f891d619))
* **workers:** add WorkerStatusType enum ([c8a9fbd](https://github.com/tgiachi/squid-std/commit/c8a9fbd2e504b0e3a2a772791dfd828882712721))
* **workers:** carry ActiveJobs and MaxConcurrency in heartbeat and worker info ([4fa8674](https://github.com/tgiachi/squid-std/commit/4fa86748d8f338f176df86ecc8223d2a23f10819))

## [0.3.0](https://github.com/tgiachi/squid-std/compare/v0.2.0...v0.3.0) (2026-06-23)

### Features

* **aspnetcore:** add AddSquidStdHealthChecks to bridge health checks to ASP.NET Core ([0d94095](https://github.com/tgiachi/squid-std/commit/0d94095b82c2116b87a0eb526604dc77abdbd1c4))
* **aspnetcore:** add SquidStdHealthCheckAdapter mapping to standard health checks ([7c1316c](https://github.com/tgiachi/squid-std/commit/7c1316c9188385adb5bfccbee41d285bd9522c39))
* **health:** add IHealthCheck contracts, HealthCheckResult/HealthReport DTOs and options ([df6ba54](https://github.com/tgiachi/squid-std/commit/df6ba54ca1f2752fd3b60c99c6a26ddf97bf4196))
* **health:** add parallel HealthCheckService aggregator with per-check timeout ([7df80c4](https://github.com/tgiachi/squid-std/commit/7df80c4b081db722451c331582366195c51db8a0))
* **health:** add RegisterHealthChecksService DI extension ([a7f7db3](https://github.com/tgiachi/squid-std/commit/a7f7db30c8f69e664daf380943ad20ec75ea0852))
* **storage:** add ListKeysAsync to IStorageService/IObjectStorageService and providers ([f6b524b](https://github.com/tgiachi/squid-std/commit/f6b524b5617a9277eba77555521ac0f64cf46704))
* **storage:** add S3/MinIO storage provider and registration ([5332b4d](https://github.com/tgiachi/squid-std/commit/5332b4d68fcbabaa841a4a71463c8b46378963ef))

### Bug Fixes

* **storage:** build the MinIO client in a helper to fix CI build break ([1718ba8](https://github.com/tgiachi/squid-std/commit/1718ba8eb437d3cd8fd963f3841438ba88e7a853))

## [0.2.0](https://github.com/tgiachi/squid-std/compare/v0.1.0...v0.2.0) (2026-06-22)

### Features

* add Services.Core, config manager, Lua scripting project and test reformatting ([7d17a58](https://github.com/tgiachi/squid-std/commit/7d17a5842f4e50ad9b87ac47fb78e14b4dac9ea1))
* add SquidStd bootstrap options ([4b2f396](https://github.com/tgiachi/squid-std/commit/4b2f3965927c6cb1155f4df5c50281afa33e9495))
* add SquidStd bootstrap orchestrator ([cc29938](https://github.com/tgiachi/squid-std/commit/cc29938532f2c99cfb2d06ba7890985783301b04))
* **aspnetcore:** add SquidStd hosted service ([321f7a1](https://github.com/tgiachi/squid-std/commit/321f7a1c56aa643a5bc858f6522aec398c20eef9))
* **aspnetcore:** add WebApplicationBuilder bridge ([ad7cda5](https://github.com/tgiachi/squid-std/commit/ad7cda5b3182216df971019964c74c30aac05940))
* **bootstrap:** support external DryIoc containers ([3ce9333](https://github.com/tgiachi/squid-std/commit/3ce93338d8e1fb95c20f96fa0c85aae39daf26f8))
* **caching:** add CacheMetricsProvider and NoOpCacheMetrics ([81f6276](https://github.com/tgiachi/squid-std/commit/81f6276897a6acc293ac81ef382de1cc01313985))
* **caching:** add Caching.Abstractions interfaces, options and connection string ([8e25a93](https://github.com/tgiachi/squid-std/commit/8e25a935ad6e2b2aebad6a5d0cc4ffb7a1b221ab))
* **caching:** add in-memory cache provider and registration ([cde6ea6](https://github.com/tgiachi/squid-std/commit/cde6ea6fa572b1f1f1f2d18441d61616dee215b7))
* **caching:** add Redis cache provider and registration ([4fdc868](https://github.com/tgiachi/squid-std/commit/4fdc8684f260c001c91cfb61027ef155fabf1afc))
* **caching:** add typed CacheService facade with cache-aside ([2b3d0bf](https://github.com/tgiachi/squid-std/commit/2b3d0bf48914ffc8d55d4089fd63c0d123d08555))
* **config:** substitute $ENV_VAR tokens in string config values on load ([b6b7eab](https://github.com/tgiachi/squid-std/commit/b6b7eabb37b78d41d1682f546c1e9184251ce46b))
* configure bootstrap logging sinks ([3c03f22](https://github.com/tgiachi/squid-std/commit/3c03f22c26efe8a15d279f6626d89ecadaeee80f))
* **core:** add IDataSerializer/IDataDeserializer with JsonDataSerializer ([8bbcdeb](https://github.com/tgiachi/squid-std/commit/8bbcdebc309b0f8d23fc2c5b28645bfd23440dff))
* **core:** add ReplaceEnv regex env-substitution extension ([d5f9117](https://github.com/tgiachi/squid-std/commit/d5f9117ae118669efbfe7673ee4e4f8f5fd7018a))
* **database:** add connection-string parser and FreeSql database service ([8649240](https://github.com/tgiachi/squid-std/commit/8649240a02c7a330fe4b426a8976e4659bf6d832))
* **database:** add DatabaseProviderType, BaseEntity and DatabaseConfig ([d9856e9](https://github.com/tgiachi/squid-std/commit/d9856e92ea4c1e29a36fce330c146a037e56512e))
* **database:** add FreeSql data access with bulk ops and transactional writes ([4a73a59](https://github.com/tgiachi/squid-std/commit/4a73a594f92d4c9e8f09b0898ca4603c59e97f51))
* **database:** add IDataAccess contract ([5b05964](https://github.com/tgiachi/squid-std/commit/5b05964a48ac6dd809a58da4ffb8519bfed15666))
* **database:** add PagedResultData DTO with paging metadata ([e5b5162](https://github.com/tgiachi/squid-std/commit/e5b5162b0fdc345b895764c6e42a762455c97bac))
* **database:** add RegisterDatabase DI extension ([56a1f61](https://github.com/tgiachi/squid-std/commit/56a1f610e9686b3d55ef3dc1e1d87656efa14c1e))
* **database:** add SquidStd.Database project with FreeSql and ZLinq ([5a2eadf](https://github.com/tgiachi/squid-std/commit/5a2eadfcde00281da22ad32fb830bcd07cac7ca4))
* **database:** add SquidStd.Database.Abstractions project skeleton ([08268cc](https://github.com/tgiachi/squid-std/commit/08268ccf665c50e0eb65c95516aece53d6f2eda2))
* **database:** add ZLinq in-memory result helpers ([676ce91](https://github.com/tgiachi/squid-std/commit/676ce91ef1547054e7c00124621a1bc0b91a7e5e))
* **messaging:** add AddInMemoryMessaging DI registration ([74a9e37](https://github.com/tgiachi/squid-std/commit/74a9e37190be79491b37dc64a1a112c9620835ed))
* **messaging:** add in-memory queue provider with round-robin, retry and dead-letter ([583fa26](https://github.com/tgiachi/squid-std/commit/583fa268a3c9ff50758c3853d834cbafbb823e91))
* **messaging:** add messaging abstractions (contracts, options, no-op metrics) ([78bfcf2](https://github.com/tgiachi/squid-std/commit/78bfcf25eec111abdb2d9eaf5792dff6ee2d9ec4))
* **messaging:** add MessagingMetricsProvider bridging IMessagingMetrics to IMetricProvider ([4e8ddea](https://github.com/tgiachi/squid-std/commit/4e8ddea49e0a59e0f65d51317ea67bdbd884ac9d))
* **messaging:** add RabbitMq queue provider and registration ([f3deddb](https://github.com/tgiachi/squid-std/commit/f3deddbc925c52b86537db76718e90471db1dc1b))
* **messaging:** add scheme-based MessagingConnectionString and in-memory url overload ([b83bff0](https://github.com/tgiachi/squid-std/commit/b83bff02c32378d63c6da3f261d8c333fd233927))
* **messaging:** add SquidStd.Messaging project with JSON serializer ([89c0cad](https://github.com/tgiachi/squid-std/commit/89c0cad047901da598985048760ecf61f1862644))
* **messaging:** add typed MessageQueue facade over the byte-level provider ([179bce3](https://github.com/tgiachi/squid-std/commit/179bce3703a8954a1f5298356976dee6daa9ffd7))
* **metrics:** add core metrics collection service ([6f66f01](https://github.com/tgiachi/squid-std/commit/6f66f015619fe3633633a604ead84324bb20ffcc))
* **packaging:** publish Messaging, Messaging.Abstractions, Messaging.RabbitMq and Scripting.Lua with READMEs ([571c3c3](https://github.com/tgiachi/squid-std/commit/571c3c355a034f1417ca9880b3868774f1f09b3b))
* register logger config section ([8f0e595](https://github.com/tgiachi/squid-std/commit/8f0e595e68d304af2b3d6d1d642829a9ce0aea40))
* **scheduling:** add Cronos dependency, ICronScheduler and config/DTO surface ([8a53272](https://github.com/tgiachi/squid-std/commit/8a5327206ce0b95b0b743e1ac16d2ea40d2c68c9))
* **scheduling:** add CronSchedulerService with one-shot rescheduling ([41e8d07](https://github.com/tgiachi/squid-std/commit/41e8d070d185b81c2e3434931105addd7f133e84))
* **scheduling:** add RegisterSchedulerServices DI extension ([06dbfac](https://github.com/tgiachi/squid-std/commit/06dbfac532df893a64b6a203e5cb76a5fbc647a1))
* **scheduling:** add TimerWheelPumpService to advance the wheel ([d43c53e](https://github.com/tgiachi/squid-std/commit/d43c53eda93f0714197c68aebaa31293681726d3))
* **storage:** add YAML storage and encrypted secrets ([1daf366](https://github.com/tgiachi/squid-std/commit/1daf366144949c93159de4e80a6c5f1e779f6fb0))
* **udp:** add UdpSessionManager with per-endpoint sessions and idle-timeout sweep ([5568534](https://github.com/tgiachi/squid-std/commit/55685343f2838b547a77edc3fb590052a3f454e9))
* **udp:** make UDP server observable with OnDatagramReceived and targeted SendToAsync ([4f65c84](https://github.com/tgiachi/squid-std/commit/4f65c84533af6653ea627d792904db4bc76b24cd))

### Bug Fixes

* **lua:** align script engine lifecycle contract ([017934c](https://github.com/tgiachi/squid-std/commit/017934c6ac30417dbfc01a96c5d97302883b55fa))
* **messaging:** repair stale usings left by rabbitmq merge (build was broken) ([ee81bfe](https://github.com/tgiachi/squid-std/commit/ee81bfe8f977a982b4cecf3a5329de04da0ec4b9))

## [0.1.0](https://github.com/tgiachi/squid-std/compare/v0.0.0...v0.1.0) (2026-06-22)

### Features

* add config section metadata ([9988f37](https://github.com/tgiachi/squid-std/commit/9988f374479f2e2e63f6cbb8dbcdeb406887d08e))
* add config section registration ([5e1097b](https://github.com/tgiachi/squid-std/commit/5e1097b26957b8c6545da5eacebe9dd5f5cb03ae))
* add Core, Abstractions, Plugin.Abstractions, Network and Services.Core libraries ([fdcce86](https://github.com/tgiachi/squid-std/commit/fdcce86d23598b29cbfdc8e0b5c349ffec871f44))
* add singleton config manager service ([1383b5a](https://github.com/tgiachi/squid-std/commit/1383b5a02a87f4d714f6ba8711ca8fac0bf1fc85))
* add yaml config section helpers ([2b4f653](https://github.com/tgiachi/squid-std/commit/2b4f65390b4f1702fd25a98b0280dce648014ea4))
* **sessions:** add ISessionManager and SessionManager observing the TCP server ([3c31cd7](https://github.com/tgiachi/squid-std/commit/3c31cd7aa15fcf9042cd83874c2f7ca82a565b21))
* **sessions:** add session event args ([fe05932](https://github.com/tgiachi/squid-std/commit/fe05932b25bdcaa49c978d0f0f1a1059e959b661))
* **sessions:** add Session<TState> entity delegating send/close to the connection ([03a0b1c](https://github.com/tgiachi/squid-std/commit/03a0b1cde2eedfe5ddca02627ef7031f99be7149))
* wire config manager startup ([3f6bf4c](https://github.com/tgiachi/squid-std/commit/3f6bf4cc224a98c1fbd15823e55eb06a6fa37cbe))
