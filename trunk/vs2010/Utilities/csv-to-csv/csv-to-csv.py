#!/usr/bin/env python

import csv
import getopt
import sys
from sys import argv

__this_file__ = 'eyeballs.exe'

class CsvToCsv():
    def __init__(self):
        self.ReadCommandLine()

    def ReadCommandLine(self):
        filename = None
        outfile = "out.csv"
        columns = [0]
        skip_lines = 0
        prefix = ""
        postfix = ""
        try:
            # see if there are command-line arguments
            opts, args = getopt.getopt(argv[1:],'?f:c:s:o:b:e:', ['help','filename=','columns=','skip=', 'outfile=', 'begin=', 'end='])
            # scan throught the options and see what needs to be done.
            for o,a in opts:
                if o in ('-?', '--help'):
                    self.show_usage()
                if o in ('-f', '--filename'):
                    filename = a
                if o in ('-c', '--columns'):
                    columns = [int(i) for i in a.split(',')]
                if o in ('-s', '--skip'):
                    skip_lines = int(a)
                if o in ('-i', '--outfile'):
                    outfile = a
                if o in ('-b', '--begin'):
                    prefix = a
                if o in ('-e', '--end'):
                    postfix = a
        except getopt.error, msg:
            print msg
            self.show_usage()
        if filename is None:
            filename = 'STDIN' # use STDIN if 
        self.load_data(filename, outfile, columns, skip_lines, prefix, postfix)

    def show_usage(self):
        print 'usage:'
        print '   %s --filename file' % __this_file__
        print ' where filename is a comma delimited data'
        print '-c or --columns lists the columns to pull'
        print '-s or --skip to skip a fixed number of lines at the start'
        print
        
        sys.exit(0)

    def load_data(self, filename, outfile, columns, skip_lines, prefix, postfix):   
        if filename == 'STDIN':
            with sys.stdin as f:
                rows = self.read_data(f, filename, columns, skip_lines)
        else:
            with open(filename, 'rb') as f:
                rows = self.read_data(f, filename, columns, skip_lines)

        with open(outfile, 'wb') as f:
            self.write_data(f, outfile, rows, prefix, postfix)

    def read_data(self, file_obj, filename, columns, skip_lines):
        try:
            rows = []
            reader = csv.reader(file_obj)

            # skip lines before reading 
            for i in range(abs(skip_lines)):
                reader.next()

            # read the data
            for row in reader:
                data=[]
                for i in range(len(columns)):
                    data.append(row[columns[i]])
                rows.append(data)           
            return rows
            
        except csv.Error, e:
            sys.exit('file %s, line %d: %s' % (filename, reader.line_num, e))

    def write_data(self, file_obj, filename, rows, prefix, postfix):
        try:
            writer = csv.writer(file_obj, delimiter=',')
            for row in rows:
                if prefix != '':
                    row[0] = '%s%s' % (prefix, row[0])
                if postfix != '':
                    ix = len(row)-1
                    row[ix] = '%s%s' % (row[ix], postfix)                    
                writer.writerow( row)
            
        except csv.Error, e:
            sys.exit('file %s, line %d: %s' % (filename, writer.line_num, e))
    

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
    csv_to_csv = CsvToCsv()
    
