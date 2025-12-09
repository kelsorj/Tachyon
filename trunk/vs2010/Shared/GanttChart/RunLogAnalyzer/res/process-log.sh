#!/bin/bash

export logfile=$1

echo Processing logfile named $logfile and creating $logfile.csv

grep -i gantt $logfile > $logfile.grep
sed -r -f sed.sed $logfile.grep > $logfile.csv



