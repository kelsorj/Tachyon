#!/usr/bin/env python

import wx
import csv
import getopt
import sys
from sys import argv
from graph_eye import GraphWindow

__this_file__ = 'eyeballs.exe'

class EyeBalls():
    def __init__(self):
        self.ReadCommandLine()

    def ReadCommandLine(self):
        filename = None
        xcolumns = [0]
        ycolumns = [0]
        nogrid = False
        nolines = False
        nosquares = False
        header_string = ""
        skip_lines = 0
        try:
            # see if there are command-line arguments
            opts, args = getopt.getopt(argv[1:],'f:x:y:glq?h:s:', ['filename=','xcolumn=','ycolumn=','nogrid','nolines','nosquares','help','header=','skip='])
            # scan throught the options and see what needs to be done.
            for o,a in opts:
                if o in ('-?', '--help'):
                    self.show_usage()
                if o in ('-f', '--filename'):
                    filename = a
                if o in ('-x', '--xcolumn'):
                    xcolumns = [int(i) for i in a.split(',')]
                if o in ('-y', '--ycolumn'):
                    ycolumns = [int(i) for i in a.split(',')]
                if o in ('-g', '--nogrid'):
                    nogrid = True
                if o in ('-l', '--nolines'):
                    nolines = True
                if o in ('-q', '--nosquares'):
                    nosquares = True
                if o in ('-h', '--header'):
                    header_string = [i for i in a.split(',')]
                if o in ('-s', '--skip'):
                    skip_lines = int(a)
        except getopt.error, msg:
            print msg
            filename = ''
        if filename is None:
            filename = 'STDIN' # use STDIN if 
        self.load_data(filename, xcolumns, ycolumns, nogrid, nolines, nosquares, header_string, skip_lines)

    def show_usage(self):
        print 'usage:'
        print '   %s --filename file' % __this_file__
        print ' or pipe file input from a different program:'
        print '   cat filename | %s' % __this_file__
        print
        print ' where filename is a comma delimited data file w/ a single header row'
        print ' optionally add --ycolumn to specify which column to start graphing'
        print ' column zero is assumed to be a floating point timestamp'
        print 
        print '-y or --ycolumn sets the dependent column to plot (defaults to column 0)'
        print '-x or --xcolumn sets the independent column to plot ie timestamp (defaults to column 0)'
        print '     optionally, provide a comma separated list of columns'
        print
        print '-g or --nogrid turns off the grid'
        print '-l or --nolines turns off lines between points'
        print '-q or --nosquares turns off the squares drawn on points'
        print
        print '-h or --header to use a comma separated list of strings for header instead of the first line of the file'
        print
        print '-s or --skip to skip a fixed number of lines before (-) or after (+) the header line is parsed'
        print
        
        sys.exit(0)

    def load_data(self, filename, xcolumns, ycolumns, nogrid, nolines, nosquares, header_string, skip_lines):   
        try:
            if filename == 'STDIN':
                with sys.stdin as f:
                    (headers, datas) = self.read_data(f, xcolumns, ycolumns, header_string, skip_lines)
            else:
                with open(filename, 'rb') as f:
                    (headers, datas) = self.read_data(f, xcolumns, ycolumns, header_string, skip_lines)

            self._graph = GraphWindow(headers, datas)

            self._graph.graph.show_grid = not nogrid
            self._graph.graph.show_lines = not nolines
            self._graph.graph.show_squares = not nosquares

        except csv.Error, e:
            sys.exit('file %s, line %d: %s' % (filename, reader.line_num, e))

    def read_data(self, file_obj, xcolumns, ycolumns, header_string, skip_lines):
        headers = []
        datas = []
    
        # if we provided a different # of x vs y columns
        # expand or truncate the x columns to match the y
        y_count = len(ycolumns)
        x_count = len(xcolumns)
        diff = y_count - x_count
        if diff != 0:            
            new_count = x_count + diff
            xcolumns = [xcolumns[i] for i in range(min(new_count, x_count))]
            if diff > 0:
                for i in range(diff): xcolumns.append(xcolumns[-1:][0])

        for i in range(y_count):
            datas.append([])

        reader = csv.reader(file_obj)

        # if our skip_lines count is negative, skip lines before reading header
        if skip_lines < 0:
            for i in range(abs(skip_lines)):
                reader.next()

        # if we didn't provide a header string, read it from the file
        if header_string == "":
            header_string = reader.next()
        header_x = [header_string[x] for x in xcolumns]
        header_y = [header_string[y] for y in ycolumns]
        headers = [(header_x[i], header_y[i]) for i in range(y_count)]

        # if our skip_lines count is positive, skip lines after reading header
        if skip_lines > 0:
            for i in range(skip_lines):
                reader.next()

        # read the rest of the data
        for row in reader:
            for i in range(y_count):
                datas[i].append((float(row[xcolumns[i]]), float(row[ycolumns[i]])))

        return (headers, datas)
    

class Unbuffered:
    def __init__(self, stream):
        self.stream = stream
    def write(self, data):
        self.stream.write(data)
        self.stream.flush()
    def __getattr__(self, attr):
        return getattr(self.stream, attr)

if __name__ == '__main__':
    sys.stdout=Unbuffered(sys.stdout)
    app = wx.App(0) #zero if you want stderr/stdout to console instead of a window, but this seems unreliableS
    eyes = EyeBalls()
    app.MainLoop()
    
