"""
To compute hash using merkle tree
"""
import sys
import hashlib
import pathlib
from pathlib import Path
import os
import time
import collections
import matplotlib.pylab as plt

class MerkleTreeHash(object):
	def __init__(self):
		self.fileNames = [];
		self.fileHashes = [];
		
	def ComputeMerkleTreeOverVariableMessageLength(self,dirName):
		"""
		Computes hash of variable message length. 
		In this case we will be creating variable message by appending file contents
		each time to previous message.
		"""
		
		fileContents = "";
		fileContentHash = {};
		
		#iterate over directory if filenames are collected already
		if self.fileNames is not None and not self.fileNames:
			self.ReadDirectory(dirName)
		self.fileNames = sorted(self.fileNames)
		
		#read each file and append contents to previous
		for fName in self.fileNames:
			fileContents += self.ReadFile(fName)
			start = time.time()
			hashValue = self.CreateFileHash(fileContents)
			end = time.time()
			fileContentHash[len(fileContents)] = end-start
		
		return collections.OrderedDict(sorted(fileContentHash.items()))
			
	
	def ComputeMerkleTreeOverFile(self, dirName):
		"""
		Calculates merkleTree over file's content as leaf
		"""
		fileContents = [];
		#iterate over directory if filenames are collected already
		if self.fileNames is not None and not self.fileNames:
			self.ReadDirectory(dirName)
		self.fileNames = sorted(self.fileNames)
		
		#read each file and collect contents as input to the merkle tree
		for fName in self.fileNames:
			fileContents.append(self.ReadFile(fName))	
		
		#in case files are odd
		if (len(fileContents) %2 != 0):
			fileContents.append(fileContents[-1])
			
		self.fileHashes = []; #reinitiate the list
		
		for groupFileContents in [fileContents[x:x+2] for x in range(0,len(fileContents),2)]:
			self.fileHashes.append(self.CreateFileHash(groupFileContents[0] + groupFileContents[1]))
		
		return self.FindMerkleHash(self.fileHashes)
	
	def ComputeMerkleTreeOverFileHash(self, dirName):
		"""
		Calculates merkleTree over file's hash
		"""
		#iterate over directory if filenames are collected already
		if self.fileNames is not None and not self.fileNames:
			self.ReadDirectory(dirName)
		self.fileNames = sorted(self.fileNames)
		
		self.fileHashes = []; #reinitiate the list
		
		#create hash for each file
		for fName in self.fileNames:
			self.fileHashes.append(self.CreateFileHash(self.ReadFile(fName)))
		
		return self.FindMerkleHash(self.fileHashes)
		
	def ReadDirectory(self, dirName):
		"""
		Reads the filenames of the directory including subdirectories.
		Appends it in fileHashes list.
		"""
		for filename in Path(dirName).iterdir():
			if filename.is_file():
				self.fileNames.append(str(Path(filename).resolve())) #to resolve the file path as per os properties
			elif filename.is_dir():
				self.ReadDirectory(filename)
				
	def ReadFile(self, fileName):
		"""
		Read the file contents
		"""
		file = open(fileName, mode='r', encoding="Latin-1")#here Latin-1 is used since utf-8 wasn't working. In usual case it should be utf-8
		return file.read()
			
	def CreateFileHash(self, content):
		"""
		Create hash from file contents and appends it to fileHashes
		"""
		hasher = hashlib.sha256()
		hasher.update(content.encode('utf-8'))
		hexDigest = hasher.hexdigest()
		#print(hexDigest.encode('utf-8'))
		return hexDigest
		
	def FindMerkleHash(self, fileHashes):
		"""
		based on input list we will be computing merkle hash using recurssion		
		Form group of two hashes and concatinate to each other. Form their hash.
		Repeat this till only one hash is left.
		"""
		
		blocks = fileHashes;
		
		if not fileHashes:
			raise ValueError('Missing fileHashes')
		
		#for hs in sorted(fileHashes):
		#	blocks.append(hs)
			
		lengthOfHashes = len(blocks)
		
		#need to adjust hashes such that merkle tree has even number of leaves. 
		#The will be done by duplicating the last hash.
		
		if(lengthOfHashes %2 != 0):
			blocks.append(blocks[-1])
			
		lengthOfHashes = len(blocks)
		
		# Form group of two hashes
		groups = []
		
		for groupBlock in [blocks[x:x+2] for x in range(0,lengthOfHashes,2)]:
			#groupBlock is group of two hashes to be concatinated together.
			hasher = hashlib.sha256()
			hasher.update(groupBlock[0].encode('utf-8')+groupBlock[1].encode('utf-8'))
			groups.append(hasher.hexdigest())
		
		#above process needs to be recursive till left with single items
		if len(groups) == 1:
			#This will be root of Merkle tree
			return groups[0]
		else:
			#recurse on the function with list of groups as input to the function
			return self.FindMerkleHash(groups)
			
if __name__ == '__main__':

	#directory name is taken from command line input
	dirName = sys.argv[1]
	
	merkleTree = MerkleTreeHash()
	merkleTree.ReadDirectory(dirName)
	print("Files are processed in following order")
	for fileName in merkleTree.fileNames:
		print(fileName)
	
	plotValues = merkleTree.ComputeMerkleTreeOverVariableMessageLength(dirName)
	
	# for k,v in plotValues.items():
		# print(k, v)
		
	x, y = zip(*sorted(plotValues.items()))
	plt.plot(x, y)
	plt.show()
	
	start = time.time()
	directFilesHash = merkleTree.ComputeMerkleTreeOverFile(dirName)
	print("Hash of Merkle tree root when started with files {0}".format(directFilesHash))
	end = time.time()
	print("Time taken for Merkle tree root when started with files {0}".format(end-start))
	
	start = time.time()
	directFilesHash = merkleTree.ComputeMerkleTreeOverFileHash(dirName)
	print("Hash of Merkle tree root when started with hash {0}".format(directFilesHash))	
	end = time.time()
	print("Time taken for Merkle tree root when started with files {0}".format(end-start))
	
	
