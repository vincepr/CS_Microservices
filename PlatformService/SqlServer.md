# part5/1 - creating Sql Server inside Kubernetes

## Terminology
1. Persistent Volume Claim - we stake in our yaml file that we need some persistent storage
2. Persistent Volume - this gets created under the hood. Ex Docker volume
3. Storage Class - our local filesystem

Since were using just a local machine, we only need to set up the first step. In AWS etc. every part would need to be configured.

## create the persistent volume claim

`local-volumeclaim.yaml`
```yaml
# the persistent volume claim. Basically to stake out real memory for the SQl-DB
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: mssql-claim
spec:
  accessModes:
    - ReadWriteMany
  resources:
    requests:
      storage: 200Mi
```

- next create our claim: (`hosepath` is the default filesystem that shows up `kubectl get storageclass`)
```
kubectl apply -f K8S/local-volumeclaim.yaml

kubectl get pvc
# NAME          STATUS   VOLUME                                     CAPACITY   ACCESS MODES   STORAGECLASS   AGE
# mssql-claim   Bound    pvc-263b1ab8-3f24-498f-b998-8aeea72f022b   200Mi      RWX            hostpath       57s
```

## use kubernetes secrets to store the sensitive data (here sql-password)
```
kubectl create secret generic mssql --from-literal=SA_PASSWORD="pa55word!"
```

## create the ms-sql-server deploy

- `mssql-plat-depl.yaml`
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mssql-depl
spec:
  replicas: 1
  selector:
    matchLabels:
      app: mssql
  template:
    metadata:
      labels:
        app: mssql
    spec:
      containers:
        - name: mssql
          image: mcr.microsoft.com/mssql/server:2017-latest
          ports:
            - containerPort: 1443
          env:
            - name: MSSQL_PID
              value: "Express"
            - name: ACCEPT_EULA
              value: "Y"
            - name: SA_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: mssql ## this name came from the `kubectl create secret generic mssql --from-literal=SA_PASSWORD="password123!"`
                  key: SA_PASSWORD
          volumeMounts:
            ## the path where in our dockercontainer the data is located at:
            - mountPath: /var/opt/mssql/data
              name: mssqldb
      volumes:
        - name: mssqldb
          persistentVolumeClaim:
            ## this references the one we created in our: `local-volumeclaim.yaml` 
            claimName: mssql-claim
---
# the DB needs to be accessible for it's Service so we create a ClusterIP for it
apiVersion: v1
kind: Service
metadata:
  name: mssql-clusterip-srv
spec:
  type: ClusterIP
  selector:
    app: mssql
  ports:
    - name: mssql
      protocol: TCP
      port: 1433
      targetPort: 1433
---
# the DB also needs to be accessible from outside the Kubernetes (at least for development)
# so we create a LoadbalancerService for it
apiVersion: v1
kind: Service
metadata:
  name: mssql-loadbalancer
spec:
  type: LoadBalancer
  selector:
    app: mssql
  ports:
  - protocol: TCP
    port: 1433
    targetPort: 1433
```

- then we fire it up:
    - we can see how it shows up in colum: `EXTERNAL-IP=localhost` so we should be able to reach there at port 1433
```
kubectl apply -f K8S/mssql-plat-depl.yaml

kubectl get services
# NAME                      TYPE           CLUSTER-IP      EXTERNAL-IP   PORT(S)          AGE
# commands-clusterip-srv    ClusterIP      10.105.102.58   <none>        80/TCP           10h
# kubernetes                ClusterIP      10.96.0.1       <none>        443/TCP          5d1h
# mssql-clusterip-srv       ClusterIP      10.105.73.193   <none>        1433/TCP         64s
# mssql-loadbalancer        LoadBalancer   10.103.72.144   localhost     1433:31483/TCP   64s
# platformnpservice-srv     NodePort       10.103.51.73    <none>        80:30085/TCP     4d1h
# platforms-clusterip-srv   ClusterIP      10.97.239.139   <none>        80/TCP           10h
```

![Alt text](./img/sqlLogin.png)


## part5/2