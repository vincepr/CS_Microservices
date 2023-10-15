## path to the CommandsService folder
CPATH=CommandsService
## path to the PlatformService folder
PPATH=PlatformService
## path to the file with K8S deployments
KPATH=K8S

# short list of status info 
status:
	kubectl get services
	kubectl get pods
	kubectl get deployments

# roll out new services (after building them with "make dev" for example)
kubectl-services:
	kubectl apply -f $(KPATH)/platforms-depl.yaml
	kubectl apply -f $(KPATH)/commands-depl.yaml
	kubectl rollout restart deployment platforms-depl
	kubectl rollout restart deployment commands-depl
	kubectl get services
	kubectl get pods

# builds everything. 
## On a new PC we also must do the 2 above steps before:
## - kubectl create secret generic mssql --from-literal=SA_PASSWORD="pa55word!"
## - we add the folowing line to `C:\Windows\System32\drivers\etc\hosts` -> "127.0.0.1 acme.com"
kubectl-all: kubectl-services
	kubectl apply -f $(KPATH)/commands-depl.yaml
	kubectl rollout restart deployment commands-depl
	kubectl apply -f $(KPATH)/platforms-depl.yaml
	kubectl rollout restart deployment platforms-depl
	kubectl apply -f $(KPATH)/patforms-np-srv.yaml
	kubectl apply -f $(KPATH)/ingress-srv.yaml
	kubectl rollout restart deployment --namespace=ingress-nginx ingress-nginx-controller
	kubectl apply -f $(KPATH)/local-volumeclaim.yaml
	kubectl apply -f $(KPATH)/mssql-plat-depl.yaml
	kubectl rollout restart deployment mssql-depl


# build dockerfiles
dev: build push

build: dockerbuildcservice dockerbuildpservice

dockerbuildcservice:
	docker build -t vincepr/commandservice $(CPATH)

dockerbuildpservice:
	docker build -t vincepr/platformservice $(PPATH)

## push dockerfiles to hub
push:
	docker push vincepr/commandservice
	docker push vincepr/platformservice

## lists exhaustive status info
statuslong:
	kubectl get secrets
	kubectl get namespaces
	kubectl get storageclass
	kubectl get pvc
	kubectl get deployments -A
	kubectl get services -A
	kubectl get pods -A
