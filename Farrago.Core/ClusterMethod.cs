namespace Farrago.Core;

public enum ClusterMethod
{
    None,
    SingleNode,
    Database,
    Consul,
    ZooKeeper,
    Redis,
    Kubernetes
}