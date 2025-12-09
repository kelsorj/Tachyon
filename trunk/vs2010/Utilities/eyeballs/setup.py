#!/usr/bin/env python

# 
# py2exe setup file
# usage:
# python setup.py py2exe
#

from distutils.core import setup
import py2exe

#setup(console=['eyeballs.py'])

setup(
	console=[ 
		{
			"script": "eyeballs.py",
			"icon_resources": [(1, "icon_eyeballs.ico")]
		}
	]
)
