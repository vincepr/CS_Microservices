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
## - kubectl create secret generic mssql --from-literal=SA_PASSWORD="pa55sword!"
## - we add the folowing line to `C:\Windows\System32\drivers\etc\hosts` -> "127.0.0.1 acme.com"
kubectl-build-all: kubectl-services
	kubectl apply -f $(KPATH)/local-volumeclaim.yaml
	kubectl apply -f $(KPATH)/mssql-plat-depl.yaml
	kubectl rollout restart deployment mssql-depl
	kubectl apply -f $(KPATH)/rabbitmq-depl.yaml
	kubectl rollout restart deployment rabbitmq-depl
	kubectl apply -f $(KPATH)/ingress-srv.yaml
	kubectl rollout restart deployment --namespace=ingress-nginx ingress-nginx-controller
	kubectl apply -f $(KPATH)/platforms-depl.yaml
	kubectl rollout restart deployment platforms-depl
	kubectl apply -f $(KPATH)/patforms-np-srv.yaml
	kubectl apply -f $(KPATH)/commands-depl.yaml
	kubectl rollout restart deployment commands-depl


# build dockerfiles, push them to dockerhub and rollout restarts (assuming the yaml files did NOT change)
dev: docker-buildall docker-pushall kubectl-rollout-services

kubectl-rollout-services:
	kubectl rollout restart deployment platforms-depl
	kubectl rollout restart deployment commands-depl 
	kubectl get pods
	kubectl get deployments

docker-buildall: 
	docker build -t vincepr/platformservice $(PPATH)
	docker build -t vincepr/commandservice $(CPATH)

docker-pushall:
	docker push vincepr/platformservice
	docker push vincepr/commandservice

## lists exhaustive status info
statusall:
	kubectl get secrets
	kubectl get namespaces
	kubectl get storageclass
	kubectl get pvc
	kubectl get deployments -A
	kubectl get services -A
	kubectl get pods -A

## run the dotnet projecs from root
runc:
	dotnet run --project CommandsService --launch-profile https

runp:
	dotnet run --project PlatformService --launch-profile https

## cleans up running containers etc
clean:
#	kubectl delete deployment rabbitmq-depl
#	kubectl delete deployment commands-depl
#	kubectl delete deployment platforms-depl
#	kubectl delete deployment mssql-depl
#	kubectl delete deployment --namespace=ingress-nginx ingress-nginx-controller

	kubectl delete -f $(KPATH)/patforms-np-srv.yaml
	kubectl delete -f $(KPATH)/platforms-depl.yaml
	kubectl delete -f $(KPATH)/commands-depl.yaml
	kubectl delete -f $(KPATH)/mssql-plat-depl.yaml
	kubectl delete -f $(KPATH)/rabbitmq-depl.yaml
	kubectl delete -f $(KPATH)/ingress-srv.yaml
	kubectl delete -f $(KPATH)/local-volumeclaim.yaml
	kubectl delete secret mssql