#!/bin/bash

./build-and-push.sh
pushd Charts
./install.sh
popd
