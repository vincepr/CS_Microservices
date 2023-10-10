# Kubernetes - aka K8S
## what we want out of kubernetes
Kubernetes is our Container-Orchestrator, making sure everything is running as it should

- containers get spun up in the right order
- make sure containers continue to run (ex after a crash)
- scales in the right way

## Notes on Architecture

![Alt text](./KubernetesArchitecture.excalidraw.svg)

**Cluster** - a group of servers/VM's that we run on. In this case we just have our desktop as our one Cluster we run everyhing on.

**Node** - Splits up our Cluster into more parts. Ex. in Azure/Google-Cloud we might end up with different nodes we run our stuff on. In this project we just use one Node.

**Pod** - Is used to host and run Containers. A pod can in theory run multiple containers. We will use just one Pod per service. (we only have 2 of those anyways)

### Kubernetes Services
Services that Kubernetes provides for us:

**Node Port** - used only for development purpose. Quickly maps a internal port and makes it accessible for us on our dev desktop machine.

**Cluster Ip(-Service)** - Attatches to a Pod and again Maps Ports from inside the container to ones inside our Node/Cluster. Only used for container to container communication.

**Ingress Nginx Controller** - a API Gateway. (the proper way to make our Endpoints accessible, replacing our dev only NodePort). This connects directly with the Pods (not trough the Cluster IPs)

**Ingress Nginx Load Balancer** - Connects our API Gateway to the outside

**Persistent Volume Claim** - since everything (but the SQL DB) is stateless we must use a persistent volume. This will make our DB persist trough Reboots etc...

## creating our first deploymend
- `platforms-depl.yaml`
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: platforms-depl
spec:
  # replicas are basically horizontal scaling (ex multiple api containers that run at the same time etc...)
  replicas: 1
  # selector and template are defining the template were creating
  selector:
    matchLabels:
      app: platformservice
  template:
    metadata:
      labels:
        app: platformservice
    spec:
      containers:
        ## we use our previously created docker containers here
        - name: platformservice
          image: vincepr/platformservice:latest
```

- we start DockerDesktop and make sure our Kubernetes is running
```
kubectl version
cd K8s

kubectl apply -f platforms-depl.yaml
kubectl get deployments
# NAME             READY   UP-TO-DATE   AVAILABLE   AGE
# platforms-depl   1/1     1            1           32s

kubectl get pods
# NAME                              READY   STATUS    RESTARTS   AGE
# platforms-depl-85677fb59d-nx2zq   1/1     Running   0          77s
```
- if we check our running containers in VscodeExtension, `docker ps` or in DockerDesktop now there should up our (by Kubernetes managed running container: `vincepr/platformservice@sha236......`)