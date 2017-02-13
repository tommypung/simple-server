package com.groinpunchstudios.simple;

import java.io.File;
import java.io.IOException;
import java.io.PrintWriter;
import java.util.logging.Level;
import java.util.logging.Logger;

public class StatusFile implements Runnable
{
	private static final Logger LOG = Logger.getLogger(StatusFile.class.getName());
	private Thread thread;
	private File file;

	public StatusFile(File file) throws IOException
	{
		this.file = file;
		file.createNewFile();

		if (!file.canWrite())
			throw new IOException("Cannot write file");

		thread = new Thread(this);
		thread.start();
	}

	@Override
	public void run()
	{
		LOG.info("Creating StatusFile: " + file);
		while(true) {
			try {
				generateStatusFile();
			} catch (IOException e) {
				LOG.log(Level.SEVERE, "StatusFile stopped working", e);
				return;
			}

			try {
				Thread.sleep(2000);
			} catch (InterruptedException e) {
			}
		}
	}

	private void generateStatusFile() throws IOException
	{
		PrintWriter pw = new PrintWriter(file);
		try {
			for(Server server : Server.getAllServers())
			{
				pw.println("     [ UDP:" + server.port + "]");
				for(Player player : server.players.values())
				{
					pw.println(
							"pos:" + player.x +
							"x" + player.y +
							" dir:" + player.dx +
							"x" + player.dy +
							" hp:" + player.hp +
							", " + new String(player.name, "UTF-8") +
							", latency: " + player.getLatency() + "ms" +
							", numPackets: " + player.getNumUpdatesReceived() +
							((player.isAlive()) ? "" : " - xxx DEAD xxx")
							);
				}
			}
		} catch(Exception e) {
			LOG.log(Level.WARNING, "Could not generate StatusFile", e);
		}
		pw.close();
	}
}
