
# Deploy App as Docker image to AWS

Followed steps from:  
[How to Deploy a Docker App to AWS using Elastic Container Service (ECS) - YouTube](https://www.youtube.com/watch?v=zs3tyVgiBQQ)

```
Docker-image -----> ECR -----> ECS
             Upload

ECR - Elastic Container Registry
ECS - Elastic Container Service
```

## Prerequisites

- AWS CLI installed - [How to install and configure the AWS CLI on Windows 10 - YouTube](https://www.youtube.com/watch?v=jCHOsMPbcV0)
- CLI configured with a user having specific ESR & ECS permissions

## Steps
- Create ECR Repository: demo-hub-repo

- Login to ECR
```
aws ecr get-login-password --region REGIONHERE!!!! | docker login --username AWS --password-stdin ACCOUNTIDHERE!!!!.dkr.ecr.REGIONHERE!!!.amazonaws.com
```
- Tag the version
```
docker tag test:latest YOURACCOUNT.dkr.ecr.YOURREGION-1.amazonaws.com/YOURREPO:YOURTAG
```

- Upload Docker image:
```
docker push YOURACCOUNT.dkr.ecr.YOURREGION.amazonaws.com/YOURREPO:YOURTAG
```



