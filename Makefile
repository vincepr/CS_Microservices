## path to the CommandsService folder
CPATH=CommandsService
## path to the PlatformService folder
PPATH=PlatformService
## path to the file with K8S deployments
KPATH=K8S

# roll out newest kubernetes
kubectl:
	kubectl apply -f $(KPATH)/platforms-depl.yaml
	kubectl apply -f $(KPATH)/commands-depl.yaml
	kubectl rollout restart deployment platforms-depl
	kubectl rollout restart deployment commands-depl
	kubectl get services
	kubectl get pods

# build dockerfiles
build: dockerbuildcservice dockerbuildpservice
dockerbuildcservice:
	docker build -t vincepr/commandservice $(CPATH)

dockerbuildpservice:
	docker build -t vincepr/platformservice $(PPATH)
# push dockerfiles to hub
push:
	docker push vincepr/commandservice
	docker push vincepr/platformservice
