# AWS CLI for Azure CLI people

Reference for working the Cinema backend on AWS, written against `aws-cli/2.36.30`.

---

## 1. The one thing that unlocks everything

`az` is a hand-curated CLI. Someone at Microsoft decided `az webapp up` should exist and wrote it.

`aws` is a generated mirror of the AWS API. Every command is one API operation, kebab-cased:

| API operation | CLI command |
|---|---|
| `CreateUserPool` | `aws cognito-idp create-user-pool` |
| `DescribeDBInstances` | `aws rds describe-db-instances` |
| `ListObjectsV2` | `aws s3api list-objects-v2` |

Consequences:

- The API reference **is** the CLI reference. Find the operation, kebab-case it, PascalCase params become `--kebab-case`.
- There is no `aws webapp up`. No convenience wrappers, no orchestration. One command, one API call. Multi-step work is multi-command, or CloudFormation.
- Verb prefixes are consistent and worth memorising: `create-` `delete-` `update-` `put-` `describe-` `list-` `get-` `tag-`.

The exception is `aws s3` (as opposed to `aws s3api`), which is a hand-written convenience layer with `cp`, `sync`, `ls`. It is the only one of its kind.

---

## 2. Mental model differences

### Resource groups do not exist

The biggest one. In Azure, every resource lives in exactly one resource group, and `az group delete` cascades.

AWS has no equivalent container. Resources are loose in an account and region. There is no cascading delete.

What people use instead:

| Need | AWS answer |
|---|---|
| Group resources logically | **Tags.** `--tags Key=Project,Value=cinema` on everything |
| Find everything tagged | `aws resourcegroupstaggingapi get-resources --tag-filters Key=Project,Values=cinema` |
| Delete a whole environment | **CloudFormation stack.** `aws cloudformation delete-stack` is the closest thing to `az group delete` |
| Hard isolation | A separate **AWS account** (via Organizations) |

Practical rule: tag everything from day one, or you will not be able to find or bill your own resources.

### Subscription is Account, and switching is a profile

| Azure | AWS |
|---|---|
| Tenant | Organization |
| Subscription | Account (a 12-digit number) |
| `az account set --subscription X` | `export AWS_PROFILE=x` or `--profile x` |
| `az account show` | `aws sts get-caller-identity` |
| `az account list` | `aws configure list-profiles` |

There is no "current subscription" server-side state. Everything is local config in `~/.aws/config` and `~/.aws/credentials`.

### Region is mandatory and per-service

Azure resources have a `--location` but the CLI does not need one to list things. AWS is different: nearly every service is regional, and a command with no region fails.

Resolution order: `--region` flag, then `AWS_REGION`, then the profile's `region`, then error.

`aws s3 ls` listing buckets from every region is a rare global exception. IAM, Route 53, and CloudFront are also global.

### ARNs replace resource IDs

Azure: `/subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Web/sites/{name}`

AWS: `arn:aws:cognito-idp:us-east-1:381491992080:userpool/us-east-1_AbC123`

Format is `arn:partition:service:region:account-id:resource`. Region and account are empty for global services (`arn:aws:iam::381491992080:role/CinemaApi`).

### Managed identity is an IAM role

| Azure | AWS |
|---|---|
| Managed identity on App Service | IAM role attached to the compute (EC2 instance profile, ECS task role, EKS IRSA, Lambda execution role) |
| RBAC role assignment | IAM policy attached to a role/user/group |
| Scope = subscription / RG / resource | Scope = the `Resource` ARN list inside the policy document |

Same idea, but AWS policies are JSON documents rather than named built-in roles, and they are far more granular. `Deny` always beats `Allow`.

---

## 3. Command translation

### Auth and context

| Azure | AWS |
|---|---|
| `az login` | `aws login` (browser, console credentials) or `aws sso login --profile x` |
| `az login --service-principal` | `aws configure` (static keys), or assume a role |
| `az account show` | `aws sts get-caller-identity` |
| `az account list-locations` | `aws ec2 describe-regions --output table` |
| `az logout` | `aws login --logout` (v2.36+) or delete the cache dir |
| `az configure` | `aws configure` / edit `~/.aws/config` |

`aws login` is new. It exchanges a Management Console session for temporary credentials and auto-refreshes them. That is what this machine is set up for.

### Discovery and help

| Azure | AWS |
|---|---|
| `az <group> --help` | `aws <service> help` |
| `az <group> <cmd> --help` | `aws <service> <operation> help` |
| `az find "..."` | No equivalent. Use `aws help` + the API docs |
| `az interactive` | `aws <service> <op> --cli-auto-prompt` |

Help opens in a pager. Pipe through `col -b` to get clean text: `aws cognito-idp create-user-pool help | col -b | less`.

### Output and querying

Good news: **both use JMESPath**. Your `--query` knowledge transfers verbatim.

| Both | Meaning |
|---|---|
| `--output table` | Human-readable table |
| `--output json` | Default for `aws` |
| `--output text` | Tab-separated, for shell pipelines |
| `--output yaml` | Available in both |
| `--query 'X[].Y'` | Same JMESPath syntax |

```bash
aws rds describe-db-instances \
  --query 'DBInstances[].{Name:DBInstanceIdentifier,Engine:Engine,Status:DBInstanceStatus}' \
  --output table
```

`--output text` is the AWS idiom for scripting, replacing `az ... -o tsv`.

### Waiting for things

| Azure | AWS |
|---|---|
| `az <group> wait --created` | `aws <service> wait <state>` |

```bash
aws rds wait db-instance-available --db-instance-identifier cinema-db
```

Waiters poll with sensible backoff and exit non-zero on timeout. `aws <service> wait help` lists the states available.

### Input from files

| Azure | AWS |
|---|---|
| `--parameters @file.json` | `--cli-input-json file://file.json` |
| inline JSON string | `file://path` for text, `fileb://path` for binary |

Two flags with no Azure equivalent, both worth knowing:

```bash
aws cognito-idp create-user-pool --generate-cli-skeleton > pool.json
# fill it in
aws cognito-idp create-user-pool --cli-input-json file://pool.json
```

`--generate-cli-skeleton` prints the full request shape with every optional field. It is the fastest way to learn any AWS API.

### Infrastructure as code

| Azure | AWS |
|---|---|
| ARM / Bicep template | CloudFormation template (YAML or JSON) |
| `az deployment group create` | `aws cloudformation deploy` |
| `az deployment group what-if` | `aws cloudformation deploy --no-execute-changeset` then `describe-change-set` |
| `az group delete` | `aws cloudformation delete-stack` |
| Terraform | Terraform, or AWS CDK (real C#, worth a look given your stack) |

There is no universal `--dry-run`. EC2 operations have one; nothing else does. Change sets are the general answer.

---

## 4. Service map for this project

| Azure | AWS | CLI namespace |
|---|---|---|
| Entra ID B2C | Cognito user pool | `aws cognito-idp` |
| Azure Database for PostgreSQL | RDS PostgreSQL | `aws rds` |
| App Service / Container Apps | ECS Fargate / App Runner | `aws ecs` / `aws apprunner` |
| Azure Container Registry | ECR | `aws ecr` |
| Key Vault | Secrets Manager / Parameter Store | `aws secretsmanager` / `aws ssm` |
| Azure Monitor / Log Analytics | CloudWatch | `aws logs` / `aws cloudwatch` |
| Communication Services Email | SES | `aws sesv2` |
| Blob Storage | S3 | `aws s3` / `aws s3api` |
| Functions | Lambda | `aws lambda` |
| Service Bus | SQS / SNS / EventBridge | `aws sqs` / `aws sns` / `aws events` |
| Cosmos DB | DynamoDB | `aws dynamodb` |
| Front Door / CDN | CloudFront | `aws cloudfront` |
| VNet | VPC | `aws ec2` |
| Network Security Group | Security Group | `aws ec2` |
| Managed identity | IAM role | `aws iam` |

Naming traps: Cognito is `cognito-idp`, not `cognito`. SES has both `ses` (v1) and `sesv2`; use `sesv2`. VPCs and security groups live under `aws ec2`, not their own namespace.

---

## 5. Flags you will use daily

| Flag | Why |
|---|---|
| `--profile` | Switch account. Replaces `az account set` |
| `--region` | Mandatory for most services |
| `--output table` | Readability |
| `--query` | JMESPath, same as `az` |
| `--no-cli-pager` | Stop help and output opening in `less` |
| `--cli-auto-prompt` | Guided interactive mode |
| `--generate-cli-skeleton` | Print the full request shape |
| `--cli-input-json file://x` | Feed a request from a file |
| `--max-items` / `--no-paginate` | AWS CLI auto-paginates by default |
| `--dry-run` | EC2 only. Do not expect it elsewhere |

Useful in `~/.aws/config`:

```ini
[default]
region = us-east-1
output = table
cli_pager =
```

Empty `cli_pager` disables `less` globally, which is the single most annoying default for someone coming from `az`.

---

## 6. Gotchas that bite Azure people

1. **Deleting is not cascading.** Deleting an RDS instance leaves its subnet group, security group, parameter group, and final snapshot behind. All bill separately. There is no resource group to nuke.
2. **Auto-pagination hides cost.** `aws logs filter-log-events` with no bound will happily make hundreds of API calls. Use `--max-items`.
3. **Deny beats Allow, always.** An explicit `Deny` in any attached policy or SCP wins regardless of how many `Allow`s exist. This is unlike Azure RBAC's additive model.
4. **Eventual consistency on IAM.** A role created a second ago may not be assumable yet. Retry, or use a waiter.
5. **`aws s3` vs `aws s3api`.** The first is the friendly wrapper (`cp`, `sync`), the second is the raw API. Different flags, different behaviour.
6. **Region-scoped names.** An S3 bucket name is globally unique across all AWS customers. A Cognito pool name is not. Rules vary per service.
7. **Root credentials.** `~/.aws/config` here has `login_session = arn:aws:iam::381491992080:root`. Azure has no real equivalent of root, so this is easy to miss. Root cannot be scoped, cannot be restricted by policy, and cannot be revoked without changing the account password. Create an IAM user or role for daily work.

---

## 7. Do it now: Cognito for Cinema

Sanity check, replacing `az account show`:

```bash
aws login
aws sts get-caller-identity
```

Create the user pool (step 3 of the backend plan):

```bash
aws cognito-idp create-user-pool \
  --pool-name cinema-dev \
  --auto-verified-attributes email \
  --username-attributes email \
  --user-pool-tags Project=cinema,Env=dev \
  --query 'UserPool.Id' --output text
```

Create the app client. Public client, so no secret:

```bash
aws cognito-idp create-user-pool-client \
  --user-pool-id us-east-1_XXXXXXXXX \
  --client-name cinema-web \
  --no-generate-secret \
  --explicit-auth-flows ALLOW_USER_SRP_AUTH ALLOW_REFRESH_TOKEN_AUTH \
  --query 'UserPoolClient.ClientId' --output text
```

Confirm the discovery document the JWT middleware will fetch:

```bash
curl -s https://cognito-idp.us-east-1.amazonaws.com/us-east-1_XXXXXXXXX/.well-known/openid-configuration | jq .
```

Those two IDs go into `appsettings.Development.json` under a `Cognito` section. Neither is a secret.

Tear down:

```bash
aws cognito-idp delete-user-pool --user-pool-id us-east-1_XXXXXXXXX
```

---

## 8. Where to look things up

- `aws <service> help` for the operation list
- `aws <service> <operation> help` for parameters
- `aws <service> <operation> --generate-cli-skeleton` for the full request shape
- AWS API reference for the service, since operation names map 1:1
