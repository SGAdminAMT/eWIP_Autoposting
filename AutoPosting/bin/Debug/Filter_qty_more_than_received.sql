select _job, _oper_num, count(*)		--this part is selecting duplicate rows in tbjobtransmain
From [eWIP_AMT].[dbo].tbJobTransMain
Group By _job, _oper_num
Having count(*) > 1

select (_job + cast(_oper_num AS varchar(10))) as job_opernum, count(*) --this just concat two columns
From [eWIP_AMT].[dbo].tbJobTransMain
Group By _job, _oper_num
Having count(*) > 1

-- This part is selecting the rows whose ewip complete qty + ewip scrapped qty is larger than the received qty
select [job_mst].job, [job_mst].suffix, [job_mst].job_date,[job_mst].ord_type, [job_mst].item, [job_mst].stat,[jobroute_mst].[oper_num]
      ,[jobroute_mst].[wc]
      ,[jobroute_mst].[complete]
	  ,[jobroute_mst].[bflush_type]
	  ,[jobroute_mst].[cntrl_point]
      ,[jobroute_mst].[qty_scrapped]
      ,[jobroute_mst].[qty_received]
      ,[jobroute_mst].[qty_moved]
      ,[jobroute_mst].[qty_complete]
	  ,[tbJobTransMain].[_qty_complete] as "main_comp"
	  ,[tbJobTransMain].[_qty_scrapped] as "main_scra"
	  ,[tbJobTransMain].[_tansnum]
	  ,[jrt_sch_mst].[sched_drv]
	  ,[jrt_sch_mst].run_mch_hrs
	  ,[jrt_sch_mst].run_lbr_hrs
	  ,[jobroute_mst].fovhd_rate_mch
	  ,[jobroute_mst].fixovhd_rate
	  ,[jobroute_mst].vovhd_rate_mch
	  ,[jobroute_mst].varovhd_rate
	  ,maxtable.last_oper
	  ,[item_mst].Uf_FIFO_FEFO
	  ,[tbJobTransMain]._nextoper_num
	  ,[tbJobTransMain]._start_time
	  ,[tbJobTransMain]._end_time
	  ,[item_mst].shelf_life
	  ,[jobroute_mst].run_rate_lbr
	  ,[jobroute_mst].fixovhd_t_mch
	  ,[jobroute_mst].fixovhd_t_lbr
	  ,[jobroute_mst].varovhd_t_mch
	  ,[jobroute_mst].varovhd_t_lbr
	  ,[job_mst].revision
	  ,[jobroute_mst].run_hrs_t_mch
	  ,[jobroute_mst].run_hrs_t_lbr
	  ,[jobroute_mst].run_cost_t_lbr
    FROM [AMTLive].[dbo].[job_mst]       
	inner join  [AMTLive].[dbo].[jobroute_mst]
	on [job_mst].job = [jobroute_mst].job and  [job_mst].suffix = [jobroute_mst].suffix
	inner join [eWIP_AMT].[dbo].[tbJobTransMain]
	on [job_mst].job = [tbJobTransMain]._job and [jobroute_mst].[oper_num] = [tbJobTransMain]._oper_num
	left join [AMTLive].[dbo].[jrt_sch_mst]
	on  [job_mst].[job] = [jrt_sch_mst].[job] and [jrt_sch_mst].[oper_num] = [jobroute_mst].[oper_num]
	left join (select [jobroute_mst].job,max([jobroute_mst].oper_num) as last_oper From [AMTLive].[dbo].[jobroute_mst] Group By [jobroute_mst].job) as maxtable
	on [job_mst].job = maxtable.job
	left join [AMTLive].[dbo].[item_mst]
	on [job_mst].item = [item_mst].item
	left join [eWIP_AMT].[dbo].[tbJobRoute]
	on [jobroute_mst].job = [tbJobRoute]._job and [jobroute_mst].oper_num = [tbJobRoute]._operationNo
	where [job_mst].type='J' and [job_mst].stat='R' and [jobroute_mst].complete = 0 and [jobroute_mst].[bflush_type] = 'N' and [jobroute_mst].[cntrl_point] = 1 and ([jobroute_mst].[qty_complete] <> [tbJobTransMain].[_qty_complete] or [jobroute_mst].[qty_scrapped] <> [tbJobTransMain].[_qty_scrapped])
	and ([tbJobTransMain].[_qty_complete]+ [tbJobTransMain].[_qty_scrapped]) > [jobroute_mst].[qty_received]