#!/bin/bash
helm upgrade --install farrago farrago/ --values farrago/values.yaml --namespace=farrago --create-namespace
